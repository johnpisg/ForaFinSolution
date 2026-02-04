using ForaFin.CompaniesApi.Domain;
using Microsoft.EntityFrameworkCore;

namespace ForaFin.CompaniesApi.Infrastructure.Persistence;

public class ForaFinDb : DbContext
{
    public ForaFinDb(DbContextOptions<ForaFinDb> options) : base(options) { }
    public DbSet<ForaFinCompany> Companies => Set<ForaFinCompany>();
    public DbSet<ForaFinCompanyIncomeInfo> CompanyIncomeInfos => Set<ForaFinCompanyIncomeInfo>();
}