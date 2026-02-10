using ForaFinServices.Domain;
using ForaFinServices.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ForaFinServices.Infrastructure.Persistence;

public class ForaFinRepository: IForaFinRepository
{
    private readonly ForaFinDb _dbContext;

    public ForaFinRepository(ForaFinDb dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<ForaFinCompany>> GetAllAsync(string startsWith, CancellationToken ct = default)
    {
        var query = _dbContext.Companies.AsNoTracking().AsQueryable();

        if (!string.IsNullOrEmpty(startsWith))
        {
            startsWith = startsWith.ToLower();
            query = query.Where(c => c.Name.ToLower().StartsWith(startsWith));
        }

        return await query.Include(c => c.IncomeInfos).OrderBy(c => c.Name).ToListAsync(ct);
    }

    public async Task AddAsync(ForaFinCompany item)
    {
        foreach (var incomeInfo in item.IncomeInfos)
        {
            incomeInfo.Company = item;
            await _dbContext.CompanyIncomeInfos.AddAsync(incomeInfo);
        }
        await _dbContext.Companies.AddAsync(item);
    }

    public async Task AddRangeAsync(IEnumerable<ForaFinCompany> items, CancellationToken ct = default)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
            try
            {
                _dbContext.ChangeTracker.AutoDetectChangesEnabled = false;
                await _dbContext.Companies.AddRangeAsync(items, ct);
                await _dbContext.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }
    public async Task SaveChangesAsync()
    {
        await _dbContext.SaveChangesAsync();
    }
}