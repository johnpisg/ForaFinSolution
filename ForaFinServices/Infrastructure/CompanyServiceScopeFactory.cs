using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain;
using ForaFinServices.Domain.Interfaces;
using ForaFinServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ForaFinServices.Infrastructure;
public class CompanyServiceScopeFactory : CompanyService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    public CompanyServiceScopeFactory(ISecEdgarService secEdgarService, IConfiguration configuration,
                IForaFinRepository foraFinRepository, IBgTaskRepository bgTaskRepository, ILogger<CompanyService> logger, IServiceScopeFactory serviceScopeFactory)
    :base(secEdgarService, configuration, foraFinRepository, bgTaskRepository, logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }
    
    public override async Task<BgTask> UpdateBgTaskStatusAsync(Guid id, BgTaskStatus newStatus, string comment = "")
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();

        var task = await context.BgTasks.FindAsync(id);
        if (task != null)
        {
            task.Status = newStatus;
            task.Comments = comment;
            task.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        return task;
    }

    public override async Task AddCompaniesBatchAsync(IEnumerable<ForaFinCompany> companies, CancellationToken ct)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();
        await context.Companies.AddRangeAsync(companies, ct);
        await context.SaveChangesAsync(ct);
    }

    public override async Task<List<BgTask>> GetOrphansBgTasksAsync(CancellationToken ct = default)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();
        return await context.BgTasks
                .Where(t => t.Status == BgTaskStatus.Created || t.Status == BgTaskStatus.Processing)
                .ToListAsync(ct);
    }
}