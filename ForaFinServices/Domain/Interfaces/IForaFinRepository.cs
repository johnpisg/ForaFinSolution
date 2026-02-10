
namespace ForaFinServices.Domain.Interfaces;
public interface IForaFinRepository
{
    Task<IEnumerable<ForaFinCompany>> GetAllAsync(string startsWith, CancellationToken ct = default);
    Task AddAsync(ForaFinCompany item);
    Task AddRangeAsync(IEnumerable<ForaFinCompany> items, CancellationToken ct = default);
    Task SaveChangesAsync();
}