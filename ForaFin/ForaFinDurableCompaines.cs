using System.Collections.Concurrent;
using System.Security.Claims;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

public class ForaFinDurableCompanies
{
    private readonly ILogger<ForaFinDurableCompanies> _logger;
    private readonly ICompanyService _companyService;

    public ForaFinDurableCompanies(ILoggerFactory loggerFactory, ICompanyService companyService)
    {
        _companyService = companyService;
        _logger = loggerFactory.CreateLogger<ForaFinDurableCompanies>();
    }

    [Function("ImportCompanies_HttpStart")]
    public async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        var user = req.FunctionContext.Items["User"] as ClaimsPrincipal;
        string instanceId = await client.ScheduleNewOrchestrationInstanceAsync("CompanyImportOrchestrator");
        _logger.LogInformation("Iniciada orquestación con ID = '{instanceId}'.", instanceId);
        return await client.CreateCheckStatusResponseAsync(req, instanceId);
    }

    [Function("CompanyImportOrchestrator")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var outputs = new List<string>();
        outputs.Add(await context.CallActivityAsync<string>("ImportCompanies_Activity", null));
        return $"Proceso completado: {string.Join(", ", outputs)}";
    }

    [Function("ImportCompanies_Activity")]
    public async Task<string> DoWork([ActivityTrigger] string name, FunctionContext executionContext, CancellationToken ct)
    {
        //await _companyService.ImportCompaniesAsync(ct);
        _logger.LogInformation("Retrieving list of CIKs to process.");
        var cikList = await _companyService.GetAllCikAsync(ct);
        _logger.LogInformation("Total CIKs retrieved: {TotalCiks}", cikList.Length);
        _logger.LogInformation("Checking which CIKs are not stored in the database");
        var cikListToRequest = await _companyService.GetNotStoredCiks(cikList.ToList(), ct);
        _logger.LogInformation("Starting import of {TotalCiks} CIKs.", cikListToRequest.Count);
        var totalCiks = cikListToRequest.Count;
        var importedCiks = 0;

        var options = new ParallelOptions {
            MaxDegreeOfParallelism = 5,
            CancellationToken = ct
        };
        var downloadedCompanies = new ConcurrentBag<ForaFinCompany>();
        await Parallel.ForEachAsync(cikListToRequest, options, async (cik, token) =>
        {
            try
            {
                _logger.LogInformation("Importing CIK {Cik}", cik);
                var importResult = await _companyService.ImportCompanyByCikAsync(cik, token);
                if(importResult != null)
                {
                    downloadedCompanies.Add(importResult);
                }
                if (downloadedCompanies.Count >= 50)
                {
                    var batch = new List<ForaFinCompany>();
                    while (downloadedCompanies.TryTake(out var item)) batch.Add(item);
                    _logger.LogInformation("Saving imported batch of {BatchCount} companies to database", batch.Count);
                    await _companyService.AddCompaniesBatchAsync(batch, token);
                }
                // Interlock para actualizar contador de forma segura entre hilos
                var current = Interlocked.Increment(ref importedCiks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing CIK {Cik}", cik);
            }
        });

        // Guardar los que sobraron al final del bucle
        if (!downloadedCompanies.IsEmpty)
        {
            await _companyService.AddCompaniesBatchAsync(downloadedCompanies, ct);
        }
        return "Empresas importadas con éxito.";
    }
}