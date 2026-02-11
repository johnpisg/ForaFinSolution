using ForaFinServices.Domain;
using ForaFinServices.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ForaFinServices.Infrastructure.Persistence;

public class BgTaskRepository: IBgTaskRepository
{
    private readonly ForaFinDb _dbContext;

    public BgTaskRepository(ForaFinDb dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(BgTask item)
    {
        _dbContext.BgTasks.Add(item);
        return Task.CompletedTask;
    }

    public Task AddRangeAsync(IEnumerable<BgTask> items, CancellationToken ct = default)
    {
        _dbContext.BgTasks.AddRange(items);
        return Task.CompletedTask;
    }

    public async Task<BgTask> GetByIdAsync(Guid guid)
    {
        return await _dbContext.BgTasks.FindAsync(guid);
    }

    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
    
    public async Task<List<BgTask>> GetOrphansBgTasksAsync(CancellationToken ct = default)
    {
        return await _dbContext.BgTasks
            .Where(t => t.Status == BgTaskStatus.Created || t.Status == BgTaskStatus.Processing)
            .ToListAsync(ct);
    }
}