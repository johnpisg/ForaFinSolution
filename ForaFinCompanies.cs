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
    private readonly ITokenValidator _tokenValidator;

    public ForaFinCompanies(ITokenValidator tokenValidator, ICompanyService companyService, JsonSerializerOptions jsonOptions, ILogger<ForaFinCompanies> logger)
    {
        _tokenValidator = tokenValidator;
        _companyService = companyService;
        _logger = logger;
        _jsonOptions = jsonOptions;
    }

    private async Task<string> GetAuthenticatedUser(HttpRequestData req)
    {
        // Extraer Header
        if (!req.Headers.TryGetValues("Authorization", out var authHeaders))
            throw new UnauthorizedAccessException("Authorization header missing.");

        var bearerToken = authHeaders.FirstOrDefault(h => h.StartsWith("Bearer "));
        if (string.IsNullOrEmpty(bearerToken))
            throw new UnauthorizedAccessException("Bearer token missing.");

        var token = bearerToken.Replace("Bearer ", "");
        var user = await _tokenValidator.ValidateTokenAsync(token);
        if (user == null)
            throw new UnauthorizedAccessException("Invalid token.");

        // Si llega aquí, el token es válido. Puedes obtener el nombre del usuario:
        var userName = user.Identity?.Name;
        return userName;
    }

    [Function("GetForaFinCompanies")]
    public async Task<IResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, CancellationToken cancellationToken)
    {
        try
        {
            await GetAuthenticatedUser(req);
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            ForaFinCompaniesInputDto? body = null;
            if (req.Body != null)
            {
                try
                {
                    body = await JsonSerializer.DeserializeAsync<ForaFinCompaniesInputDto>(req.Body, _jsonOptions);
                }
                catch { }
            }
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            var result = await _companyService.GetCompanyFactsAsync(body, vinculado.Token);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException uaEx)
        {
            _logger.LogWarning(uaEx, "Unauthorized access attempt.");
            return Results.Unauthorized();
        }
        catch (Exception ex)
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
            await GetAuthenticatedUser(req);
            _logger.LogInformation("C# HTTP trigger function ImportCompanies.");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            var msg = await _companyService.ImportCompaniesAsync(vinculado.Token);
            return Results.Ok(msg);
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

    [Function("GetAllCompanies")]
    public async Task<IResult> GetAllCompanies([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, CancellationToken cancellationToken)
    {
        try
        {
            await GetAuthenticatedUser(req);
            _logger.LogInformation("C# HTTP trigger function GetAllCompanies.");
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            var result = await _companyService.GetAllCompaniesAsync(vinculado.Token);
            return Results.Ok(result);
        }
        catch (UnauthorizedAccessException uaEx)
        {
            _logger.LogWarning(uaEx, "Unauthorized access attempt.");
            return Results.Unauthorized();
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error getting all companies.");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }

    [Function("GetToken")]
    public async Task<IResult> GetToken([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("C# HTTP trigger function GetToken.");
            var token = await _tokenValidator.GenerateTokenAsync();
            return Results.Ok(token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token.");
            return Results.Problem(ex.Message, statusCode: 400);
        }
    }
}