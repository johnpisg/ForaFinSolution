
namespace ForaFinServices.Domain.Interfaces;
public interface IForaFinRepository
{
    Task<IEnumerable<ForaFinCompany>> GetAllAsync(string startsWith, CancellationToken ct = default);
    Task AddAsync(ForaFinCompany item);
    Task AddRangeAsync(IEnumerable<ForaFinCompany> items, CancellationToken ct = default);
    Task SaveChangesAsync();
    Task<ForaFinCompany?> GetCompanyByIdAsync(int companyId, CancellationToken ct = default);
    Task<List<string>> GetNotStoredCiks(List<string> allCiks, CancellationToken ct = default);
}