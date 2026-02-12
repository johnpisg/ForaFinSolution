
using System.Configuration;
using System.Text.RegularExpressions;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain;
using ForaFinServices.Domain.Interfaces;
using ForaFinServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using static ForaFinServices.Domain.External.EdgarCompanyInfo;

namespace ForaFinServices.Infrastructure;
public class CompanyService : ICompanyService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompanyService> _logger;
    private readonly ISecEdgarService _secEdgarService;
    private readonly IForaFinRepository _foraFinRepository;
    private readonly IBgTaskRepository _bgTaskRepository;
    private readonly AsyncRetryPolicy _retryPolicy ;
    public CompanyService(ISecEdgarService secEdgarService, IConfiguration configuration,
                IForaFinRepository foraFinRepository, IBgTaskRepository bgTaskRepository, ILogger<CompanyService> logger)
    {
        _secEdgarService = secEdgarService;
        _bgTaskRepository = bgTaskRepository;
        _logger = logger;
        _configuration = configuration;
        _foraFinRepository = foraFinRepository;

        _retryPolicy = Policy.Handle<HttpRequestException>()
            .Or<OperationCanceledException>()
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    public async Task<List<ForaFinCompaniesOutputDto>> GetCompanyFactsAsync(ForaFinCompaniesInputDto? Filter, CancellationToken ct = default)
    {
        _logger.LogInformation("Fetching company facts for Filter: {Filter}", Filter?.StartsWith);
        var companies = await _foraFinRepository.GetAllAsync(Filter?.StartsWith ?? string.Empty, ct);
        var res = new List<ForaFinCompaniesOutputDto>();
        decimal tenBillion = 10_000_000_000M;
        foreach(var company in companies)
        {
            ct.ThrowIfCancellationRequested();

            var y2018_2022 = new Dictionary<int, bool>()
            {
                {2018, false },
                {2019, false },
                {2020, false },
                {2021, false },
                {2022, false }
            };
            var incomeByYear = new Dictionary<int, decimal>();
            decimal highestIncome_y2018_2022 = decimal.MinValue;
            foreach(var incomeInfo in company.IncomeInfos)
            {
                incomeByYear[incomeInfo.Year] = incomeInfo.Income;
                if(y2018_2022.Remove(incomeInfo.Year))
                {
                    if(highestIncome_y2018_2022 < incomeInfo.Income)
                    {
                        highestIncome_y2018_2022 = incomeInfo.Income;
                    }
                }
            }

            #region Standard Fundable Amount:
            decimal standardFundableAmount = 0;
            // Company must have income data for all years between (and including) 2018 and 2022.
            if(y2018_2022.Count != 0)
            {
                // If they did not, their Standard Fundable Amount is $0.
                standardFundableAmount = 0;
            }
            else if(incomeByYear[2021] <= 0 || incomeByYear[2022] <= 0)
            {
                // Company must have had positive income in both 2021 and 2022.
                // If they did not, their Standard Fundable Amount is $0.
                standardFundableAmount = 0;
            }
            else
            {
                //Using highest income between 2018 and 2022:
                if(highestIncome_y2018_2022 >= tenBillion)
                {
                    standardFundableAmount = 0.1233M * highestIncome_y2018_2022;
                } else
                {
                    standardFundableAmount = 0.2151M * highestIncome_y2018_2022;
                }
            }
            #endregion

            #region Special Fundable Amount:
            // Initially, the Special Fundable Amount is the same as Standard Fundable Amount.
            decimal specialFundableAmount = standardFundableAmount;
            // If the company name starts with a vowel, add 15% to the standard funding amount.
            if(!string.IsNullOrEmpty(company.Name) && "AEIOU".Contains(char.ToUpper(company.Name[0])))
            {
               specialFundableAmount += 0.15M * standardFundableAmount;
            }
            else if(incomeByYear.TryGetValue(2021, out decimal income2021) && incomeByYear.TryGetValue(2022, out decimal income2022))
            {
                if(income2022 < income2021)
                {
                    // If the company’s 2022 income was less than their 2021 income, subtract 25% from their standard funding amount.
                    specialFundableAmount -= 0.25M * standardFundableAmount;
                }
            }

            #endregion

            res.Add(new ForaFinCompaniesOutputDto(
                company.Id,
                company.Name,
                standardFundableAmount,
                specialFundableAmount
            ));
        }
        return res;
    }
    public async Task<string[]> GetAllCikAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Retrieving all CIKs from configuration.");
        var ciks = _configuration["SecEdgarCiks"];
        if(string.IsNullOrEmpty(ciks))
        {
            return await Task.FromResult(Array.Empty<string>());
        }
        return await Task.FromResult(ciks.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
    public virtual async Task<ForaFinCompany?> ImportCompanyByCikAsync(string cik, CancellationToken ct = default)
    {
        return await _retryPolicy.ExecuteAsync(async (token) =>
        {
            var company = default(ForaFinCompany);
            var companyFacts = await _secEdgarService.GetCompanyFactsAsync(cik, ct);
            if(companyFacts != null && !string.IsNullOrEmpty(companyFacts.EntityName))
            {
                _logger.LogInformation("Mapping company: {EntityName} with CIK: {Cik}", companyFacts.EntityName, cik);
                company = new ForaFinCompany
                {
                    Id = int.Parse(cik),
                    Name = companyFacts.EntityName
                };
                //get the income infos
                if(companyFacts.Facts?.UsGaap?.NetIncomeLoss?.Units?.Usd != null)
                {
                    foreach(InfoFactUsGaapIncomeLossUnitsUsd item in companyFacts.Facts.UsGaap.NetIncomeLoss.Units.Usd)
                    {
                        if(item.Form == "10-K" && !string.IsNullOrEmpty(item.Frame))
                        {
                            var match = Regex.Match(item.Frame, @"^CY(\d{4})$");
                            if(match.Success && item.Val > 0)
                            {
                                //get this 4 digits
                                var year = match.Groups[1].Value;
                                company.IncomeInfos.Add(new ForaFinCompanyIncomeInfo
                                {
                                    Year = int.Parse(year),
                                    Income = item.Val,
                                    CompanyId = company.Id
                                });
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.LogError("No company facts found for CIK: {Cik} or Company Name is null/empty", cik);
            }
            return company;
        }, ct);
    }
    public virtual async Task<string> ImportCompaniesAsync(CancellationToken ct = default)
    {
        var ciks = await GetAllCikAsync();
        _logger.LogInformation("Starting import of {TotalCiks} CIKs.", ciks.Length);
        var totalCiks = ciks.Length;
        var importedCiks = 0;
        var bacthSize = 10;
        foreach(var bacthCik in ciks.Chunk(bacthSize))
        {
            try
            {
                ct.ThrowIfCancellationRequested();

                var listEntities = bacthCik.Select(cik => ImportCompanyByCikAsync(cik));
                var companies = await Task.WhenAll(listEntities);
                var companiesToSave = companies.Where(c => c != null).ToList();
                if(companiesToSave != null && companiesToSave.Count > 0)
                {
                    _logger.LogInformation("Saving batch of {Count} companies to the repository.", companiesToSave.Count);
                    await _foraFinRepository.AddRangeAsync(companiesToSave!, ct);
                    importedCiks += companiesToSave.Count;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error importing company batch");
            }
        }
        return $"Imported {importedCiks} out of {totalCiks} CIKs.";
    }
    public async Task<ForaFinCompany> AddCompanyAsync(ForaFinCompany company, CancellationToken ct = default)
    {
        await _foraFinRepository.AddAsync(company);
        await _foraFinRepository.SaveChangesAsync();
        return company;
    }
    public async Task<List<ForaFinCompanyDto>> GetAllCompaniesAsync(CancellationToken ct = default)
    {
        var companies = await _foraFinRepository.GetAllAsync(string.Empty, ct);
        return companies.Select(c => new ForaFinCompanyDto(
            c.Id,
            c.Name,
            c.IncomeInfos.Select(i =>
                new ForaFinCompanyIncomeInfoDto(i.Id, i.Year, i.Income, i.CompanyId)
            ).ToList()
        )).ToList();
    }
    public async Task<bool> AnyCompanyAsync(int companyId, CancellationToken ct = default)
    {
        var company = await _foraFinRepository.GetCompanyByIdAsync(companyId, ct);
        return company != null;
    }
    public async Task<List<string>> GetNotStoredCiks(List<string> allCiks, CancellationToken ct = default)
    {
        return await _foraFinRepository.GetNotStoredCiks(allCiks, ct);
    }

    public async Task<BgTask> StoreBgTaskAsync(BgTask newTask)
    {
        await _bgTaskRepository.AddAsync(newTask);
        await _bgTaskRepository.SaveChangesAsync();
        return newTask;
    }

    public async Task<BgTask?> GetBgTaskAsync(Guid id)
    {
        return await _bgTaskRepository.GetByIdAsync(id);
    }

    public virtual async Task<BgTask> UpdateBgTaskStatusAsync(Guid id, BgTaskStatus newStatus, string comment = "")
    {
        var task = await _bgTaskRepository.GetByIdAsync(id);
        if (task != null)
        {
            task.Status = newStatus;
            task.Comments = comment;
            task.UpdatedAt = DateTime.UtcNow;
            await _bgTaskRepository.SaveChangesAsync();
        }
        return task;
    }

    public virtual async Task AddCompaniesBatchAsync(IEnumerable<ForaFinCompany> companies, CancellationToken ct)
    {
        await _foraFinRepository.AddRangeAsync(companies, ct);
        await _foraFinRepository.SaveChangesAsync();
    }

    public virtual async Task<List<BgTask>> GetOrphansBgTasksAsync(CancellationToken ct = default)
    {
        return await _bgTaskRepository.GetOrphansBgTasksAsync(ct);
    }
    public virtual async Task<ImportJob> StoreImportJobAsync(CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
    public virtual Task<string> ImportSingleCompanyAsync(StartImportRequestDto data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
}