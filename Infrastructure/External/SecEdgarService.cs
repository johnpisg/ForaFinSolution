using System.Text.Json;
using ForaFin.CompaniesApi.Application.Interfaces;
using ForaFin.CompaniesApi.Domain.External;
using Microsoft.Extensions.Logging;

namespace ForaFin.CompaniesApi.Infrastructure.External;

public class SecEdgarService : ISecEdgarService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<SecEdgarService> _logger;
    public SecEdgarService(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions, ILogger<SecEdgarService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _jsonOptions = jsonOptions;
        _logger = logger;
    }
    public async Task<EdgarCompanyInfo> GetCompanyFactsAsync(string cik, CancellationToken ct = default)
    {
        try
        {
            using(var httpClient = _httpClientFactory.CreateClient("ExternalApi"))
            {
                _logger.LogInformation("Fetching company facts for CIK: {Cik}", cik);
                var response = await httpClient.GetAsync($"CIK{cik.PadLeft(10, '0')}.json", ct);
                if (response.IsSuccessStatusCode)
                {
                    await using var stream = await response.Content.ReadAsStreamAsync(ct);
                    var edgarCompanyInfo = await JsonSerializer.DeserializeAsync<EdgarCompanyInfo>(stream, _jsonOptions, ct);
                    _logger.LogInformation("Successfully retrieved company facts for CIK: {Cik}", cik);
                    return edgarCompanyInfo;
                }
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error fetching company facts for CIK: {Cik}", cik);
            throw new InvalidOperationException($"Error fetching company facts for CIK: {cik}", ex);
        }
        _logger.LogError("Error fetching company facts for CIK: {Cik}", cik);
        return null;
    }
}
