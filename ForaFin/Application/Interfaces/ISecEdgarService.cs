using ForaFin.CompaniesApi.Domain.External;

namespace ForaFin.CompaniesApi.Application.Interfaces;
public interface ISecEdgarService
{
    Task<EdgarCompanyInfo> GetCompanyFactsAsync(string cik, CancellationToken ct = default);
}
