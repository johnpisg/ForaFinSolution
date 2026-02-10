using System.Collections.Concurrent;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain;
using ForaFinServices.Infrastructure;

namespace ForaFinApi.Utils;

public class BigProcessWorker : BackgroundService
{
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<BigProcessWorker> _logger;

    public BigProcessWorker(IBackgroundTaskQueue taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<BigProcessWorker> logger)
    {
        _taskQueue = taskQueue;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando Worker: Verificando tareas huérfanas en la base de datos...");
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();

            // Buscamos tareas que quedaron en 'Created' o 'Processing' (porque el servidor se apagó)
            var orphanTasks = await companyService.GetOrphansBgTasksAsync(cancellationToken);
            foreach (var task in orphanTasks)
            {
                _logger.LogInformation("Recuperando tarea huérfana: {TaskId}", task.Id);
                // Las re-encolamos para que ExecuteAsync las tome
                await _taskQueue.QueueBackgroundWorkItemAsync(new BgWorkItem(task.Id, task.StartsWith));
            }
        }

        // Llamamos a la base (esto inicia el ExecuteAsync)
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Waiting for background work item...");
            var workItem = await _taskQueue.DequeueAsync(stoppingToken);
            _logger.LogInformation("Dequeued background work item with TaskId: {TaskId}", workItem.TaskId);
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();

                // 1. Cambiar status a Processing
                _logger.LogInformation("Updating task status to Processing for TaskId: {TaskId}", workItem.TaskId);
                await companyService.UpdateBgTaskStatusAsync(workItem.TaskId, BgTaskStatus.Processing, "0/0 CIKs processed");
                // 2. Obtener el listado de CIKs a procesar
                _logger.LogInformation("Retrieving list of CIKs to process for TaskId: {TaskId}", workItem.TaskId);
                var cikList = await companyService.GetAllCikAsync(stoppingToken);
                _logger.LogInformation("Total CIKs retrieved: {TotalCiks} for TaskId: {TaskId}", cikList.Length, workItem.TaskId);
                // 3. Verificar cuáles CIKs no están almacenados en la BD
                _logger.LogInformation("Checking which CIKs are not stored in the database for TaskId: {TaskId}", workItem.TaskId);
                var cikListToRequest = await companyService.GetNotStoredCiks(cikList.ToList(), stoppingToken);
                _logger.LogInformation("Starting import of {TotalCiks} CIKs.", cikListToRequest.Count);
                var totalCiks = cikListToRequest.Count;
                var importedCiks = 0;

                // --- PROCESAMIENTO PARALELO ---
                // Grado de paralelismo: 5 peticiones simultáneas (para no ser bloqueados por el API externo)
                var options = new ParallelOptions {
                    MaxDegreeOfParallelism = 5,
                    CancellationToken = stoppingToken
                };
                var downloadedCompanies = new ConcurrentBag<ForaFinCompany>();
                await Parallel.ForEachAsync(cikListToRequest, options, async (cik, token) =>
                {
                    try
                    {
                        // 4. Si no existe, importarlo
                        _logger.LogInformation("Importing CIK {Cik} for TaskId: {TaskId}", cik, workItem.TaskId);
                        var importResult = await companyService.ImportCompanyByCikAsync(cik, token);
                        if(importResult != null)
                        {
                            downloadedCompanies.Add(importResult);
                        }
                        if (downloadedCompanies.Count >= 50)
                        {
                            // Extraemos los 50 actuales y limpiamos la bolsa para seguir
                            var batch = new List<ForaFinCompany>();
                            while (downloadedCompanies.TryTake(out var item)) batch.Add(item);
                            // 5. Guardar en BD
                            _logger.LogInformation("Saving imported batch of {BatchCount} companies to database for TaskId: {TaskId}", batch.Count, workItem.TaskId);
                            await companyService.AddCompaniesBatchAsync(batch, token);
                        }

                        // Interlock para actualizar contador de forma segura entre hilos
                        var current = Interlocked.Increment(ref importedCiks);

                        // 6. Actualizar status cada 10 elementos para no saturar la BD con updates
                        if (current % 10 == 0 || current == totalCiks)
                        {
                            _logger.LogInformation("Updating task status for TaskId: {TaskId} - {ImportedCiks}/{TotalCiks} CIKs processed", workItem.TaskId, importedCiks, totalCiks);
                            // Nota: UpdateBgTaskStatusAsync debe manejar su propio scope interno o ser thread-safe
                            await companyService.UpdateBgTaskStatusAsync(workItem.TaskId, BgTaskStatus.Processing, $"{current}/{totalCiks} procesados");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error importing CIK {Cik}", cik);
                    }
                });

                // Guardar los que sobraron al final del bucle
                if (!downloadedCompanies.IsEmpty)
                {
                    await companyService.AddCompaniesBatchAsync(downloadedCompanies, stoppingToken);
                }

                // 7. Una vez terminado, actualizar el status a Completed
                _logger.LogInformation("Updating task status to Completed for TaskId: {TaskId}", workItem.TaskId);
                await companyService.UpdateBgTaskStatusAsync(workItem.TaskId, BgTaskStatus.Completed, $"Import completed: {importedCiks}/{totalCiks} CIKs processed");
                _logger.LogInformation("Completed import of {TotalCiks} CIKs. Successfully imported {ImportedCiks} CIKs.", totalCiks, importedCiks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando lote de CIKs");
            }
        }
    }
}