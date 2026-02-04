using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using ForaFin.CompaniesApi.Application.Dtos;
using ForaFin.CompaniesApi.Application.Interfaces;
using Microsoft.AspNetCore.Http;


namespace ForaFin.CompaniesApi;

public class ForaFinCompanies
{
    private readonly ILogger<ForaFinCompanies> _logger;
    private readonly ICompanyService _companyService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ForaFinCompanies(ICompanyService companyService, JsonSerializerOptions jsonOptions, ILogger<ForaFinCompanies> logger)
    {
        _companyService = companyService;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    [Function("GetForaFinCompanies")]
    public async Task<IResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            ForaFinCompaniesInputDto? body = null;
            if(req.Body != null)
            {
                try
                {
                    body = await JsonSerializer.DeserializeAsync<ForaFinCompaniesInputDto>(req.Body, _jsonOptions);
                }catch{}
            }
            var result = await _companyService.GetCompanyFactsAsync(body);
            return Results.Ok(result);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error getting company facts.");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }

    [Function("ImportCompanies")]
    public async Task<IResult> ImportCompanies([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function ImportCompanies.");
            //5 minutes of timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            var msg = await _companyService.ImportCompaniesAsync(vinculado.Token);
            return Results.Ok(msg);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error importing companies.");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }

    [Function("GetAllCompanies")]
    public async Task<IResult> GetAllCompanies([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function GetAllCompanies.");
            var result = await _companyService.GetAllCompaniesAsync();
            return Results.Ok(result);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error getting all companies.");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }
}