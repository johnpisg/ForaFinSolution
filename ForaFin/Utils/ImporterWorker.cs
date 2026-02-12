using Castle.Core.Logging;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class ImporterWorker
{
    public async static Task Import<T>(IServiceProvider _services, CancellationToken stoppingToken, ILogger<T> _logger)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();
        var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();
        var queueService = scope.ServiceProvider.GetRequiredService<IQueueService>();

        var importJobs = await context.ImportJobs
            .Where(m => (m.Status == "Pending" || m.Status == "Failed") && m.Tries < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(20)
            .ToListAsync(stoppingToken);

        foreach (var job in importJobs)
        {
            try
            {
                //obtener los ciks
                var cikList = await companyService.GetAllCikAsync(stoppingToken);
                var cikListToRequest = await companyService.GetNotStoredCiks(cikList.ToList(), stoppingToken);
                var ready = true;
                foreach(var cik in cikListToRequest)
                {
                    try
                    {
                        var outbox = new OutboxMessage
                        {
                            Id = Guid.NewGuid(),
                            Type = "StartImport",
                            Content = cik,
                        };
                        context.OutboxMessages.Add(outbox);
                        await context.SaveChangesAsync(stoppingToken);
                        // SIMULACIÓN: Enviar StartImportRequestDto a la cola
                        var queueMessage = new StartImportRequestDto(outbox.Id, cik);
                        _logger.LogInformation($"Publicando mensaje a la cola...");
                        await queueService.EnqueueImportRequestAsync(queueMessage);
                    }
                    catch (Exception ex)
                    {
                        ready = false;
                        _logger.LogError(ex, $"Error publicando mensaje para CIK {cik}");
                    }
                }
                // Marcar como procesado
                job.Tries += 1;
                job.Status = ready ? "Processed" : "Failed";
                await context.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publicando mensajes para el job {JobId}", job.Id);
            }
        }
    }
}