using ForaFinServices.Domain;
using Microsoft.EntityFrameworkCore;

namespace ForaFinServices.Infrastructure.Persistence;

public class ForaFinDb : DbContext
{
    public ForaFinDb(DbContextOptions<ForaFinDb> options) : base(options) { }
    public DbSet<ForaFinCompany> Companies => Set<ForaFinCompany>();
    public DbSet<ForaFinCompanyIncomeInfo> CompanyIncomeInfos => Set<ForaFinCompanyIncomeInfo>();
    public DbSet<BgTask> BgTasks => Set<BgTask>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
}