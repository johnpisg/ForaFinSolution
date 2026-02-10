using ForaFinServices.Domain.External;

namespace ForaFinServices.Application.Interfaces;
public interface ISecEdgarService
{
    Task<EdgarCompanyInfo> GetCompanyFactsAsync(string cik, CancellationToken ct = default);
}
