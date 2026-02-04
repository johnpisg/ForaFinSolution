
namespace ForaFin.CompaniesApi.Domain.Interfaces;
public interface IForaFinRepository
{
    Task<IEnumerable<ForaFinCompany>> GetAllAsync(string startsWith);
    Task AddAsync(ForaFinCompany item);
    Task AddRangeAsync(IEnumerable<ForaFinCompany> items, CancellationToken ct = default);
    Task SaveChangesAsync();
}