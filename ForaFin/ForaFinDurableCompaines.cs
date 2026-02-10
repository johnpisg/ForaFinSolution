using System.Security.Claims;
using ForaFinServices.Application.Interfaces;
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

    // 1. CLIENTE: Recibe el HTTP POST y arranca la orquestación
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

    // 2. ORQUESTADOR: Gestiona el flujo (el "cerebro")
    [Function("CompanyImportOrchestrator")]
    public async Task<string> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var outputs = new List<string>();
        outputs.Add(await context.CallActivityAsync<string>("ImportCompanies_Activity", null));
        return $"Proceso completado: {string.Join(", ", outputs)}";
    }

    // 3. ACTIVIDAD: El trabajo pesado (el "músculo")
    [Function("ImportCompanies_Activity")]
    public async Task<string> DoWork([ActivityTrigger] string name, FunctionContext executionContext, CancellationToken ct)
    {
        await _companyService.ImportCompaniesAsync(ct);
        return "Empresas importadas con éxito.";
    }
}