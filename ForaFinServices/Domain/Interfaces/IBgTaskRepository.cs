
namespace ForaFinServices.Domain.Interfaces;
public interface IBgTaskRepository
{
    Task<BgTask> GetByIdAsync(Guid guid);
    Task AddAsync(BgTask item);
    Task AddRangeAsync(IEnumerable<BgTask> items, CancellationToken ct = default);
    Task SaveChangesAsync();
    Task<List<BgTask>> GetOrphansBgTasksAsync(CancellationToken ct = default);
}