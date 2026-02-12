using System.Collections.Concurrent;
using System.Security.Claims;
using System.Text.Json;
using DurableTask.Core;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;

public class ForaFinSaga
{
    private readonly ILogger<ForaFinSaga> _logger;
    private readonly ICompanyService _companyService;
    private readonly IServiceProvider _serviceProvider;

    public ForaFinSaga(ILoggerFactory loggerFactory, ICompanyService companyService, IServiceProvider serviceProvider)
    {
        _companyService = companyService;
        _serviceProvider = serviceProvider;
        _logger = loggerFactory.CreateLogger<ForaFinSaga>();
    }

    [Function("ImportCompanies_Saga")]
    public async Task<IResult> ImportCompanies([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function ImportCompanies_Saga.");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            var job = await _companyService.StoreImportJobAsync(vinculado.Token);
            var jobId = job.Id;
            _logger.LogInformation("ImportCompanies_Saga result: {jobId}", jobId);
            return Results.Accepted(null, new { Message = "Import Companies started", JobId = jobId });
        }
        catch (UnauthorizedAccessException uaEx)
        {
            _logger.LogWarning(uaEx, "Unauthorized access attempt.");
            return Results.Unauthorized();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error importing companies.");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }

    [Function("ImportCompanies_Saga_ProcessImport")]
    public async Task Run(
        [QueueTrigger("import-requests", Connection = "AzureWebJobsStorage")] string messageContent,
        CancellationToken ct)
    {
        //Per company CIK
        var data = JsonSerializer.Deserialize<StartImportRequestDto>(messageContent);
        var res = await _companyService.ImportSingleCompanyAsync(data, ct);
    }

    [Function("ProcessOutbox_Timer")]
    public async Task Run([TimerTrigger("*/10 * * * * *")] TimerInfo myTimer) // Cada 10 segundos
    {
        _logger.LogInformation($"ProcessOutbox_Timer triggered at: {DateTime.Now}");
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await ImporterWorker.Import(_serviceProvider, cts.Token, _logger);
    }

}
