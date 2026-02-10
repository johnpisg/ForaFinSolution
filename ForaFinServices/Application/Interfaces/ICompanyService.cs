using System.Security;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Domain;

namespace ForaFinServices.Application.Interfaces;
public interface ICompanyService
{
    Task<List<ForaFinCompaniesOutputDto>> GetCompanyFactsAsync(ForaFinCompaniesInputDto? Filter, CancellationToken ct = default);
    Task<ForaFinCompany?> ImportCompanyByCikAsync(string cik, CancellationToken ct = default);
    Task<string> ImportCompaniesAsync(CancellationToken ct = default);
    Task<ForaFinCompany> AddCompanyAsync(ForaFinCompany company, CancellationToken ct = default);
    Task<List<ForaFinCompanyDto>> GetAllCompaniesAsync(CancellationToken ct = default);
    Task<string[]> GetAllCikAsync(CancellationToken ct = default);
    Task<bool> AnyCompanyAsync(int companyId, CancellationToken ct = default);
    Task<BgTask> StoreBgTaskAsync(BgTask newTask);
    Task<BgTask?> GetBgTaskAsync(Guid id);
    Task<BgTask> UpdateBgTaskStatusAsync(Guid id, BgTaskStatus newStatus, string comment);
    Task<List<string>> GetNotStoredCiks(List<string> allCiks, CancellationToken ct = default);
    Task AddCompaniesBatchAsync(IEnumerable<ForaFinCompany> companies, CancellationToken ct);
}