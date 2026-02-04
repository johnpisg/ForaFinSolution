using ForaFin.CompaniesApi.Domain;
using ForaFin.CompaniesApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ForaFinTest;

public class ForaFinRepositoryTests : IDisposable
{
    private readonly ForaFinDb _dbContext;
    private readonly ForaFinRepository _repository;

    public ForaFinRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ForaFinDb>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _dbContext = new ForaFinDb(options);
        _repository = new ForaFinRepository(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_Filter_StartsWith()
    {
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany { Id = 1, Name = "Apple Inc", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() },
            new ForaFinCompany { Id = 2, Name = "Microsoft Corp", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() },
            new ForaFinCompany { Id = 3, Name = "Amazon Inc", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() }
        };
        await _dbContext.Companies.AddRangeAsync(companies);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAllAsync("A", CancellationToken.None);

        Assert.Equal(2, result.Count());
        Assert.Contains(result, c => c.Name == "Apple Inc");
        Assert.Contains(result, c => c.Name == "Amazon Inc");
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilter()
    {
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany { Id = 1, Name = "Apple Inc", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() },
            new ForaFinCompany { Id = 2, Name = "Microsoft Corp", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() }
        };
        await _dbContext.Companies.AddRangeAsync(companies);
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAllAsync("", CancellationToken.None);

        Assert.Equal(2, result.Count());
    }

    [Fact]
    public async Task AddRangeAsync_SaveMultipleCompanies()
    {
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany { Id = 1, Name = "Company 1", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() },
            new ForaFinCompany { Id = 2, Name = "Company 2", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() }
        };

        await _repository.AddRangeAsync(companies, CancellationToken.None);

        var count = await _dbContext.Companies.CountAsync();
        Assert.Equal(2, count);
    }
}