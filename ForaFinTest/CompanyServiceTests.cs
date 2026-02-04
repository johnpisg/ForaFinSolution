using ForaFin.CompaniesApi.Application.Dtos;
using ForaFin.CompaniesApi.Application.Interfaces;
using ForaFin.CompaniesApi.Domain;
using ForaFin.CompaniesApi.Domain.External;
using ForaFin.CompaniesApi.Domain.Interfaces;
using ForaFin.CompaniesApi.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace ForaFinTest;

public class CompanyServiceTests
{
    private readonly Mock<ISecEdgarService> _mockSecEdgarService;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IForaFinRepository> _mockRepository;
    private readonly Mock<ILogger<CompanyService>> _mockLogger;
    private readonly CompanyService _service;
    private const decimal tenBillion = 10_000_000_000M;

    public CompanyServiceTests()
    {
        _mockSecEdgarService = new Mock<ISecEdgarService>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockRepository = new Mock<IForaFinRepository>();
        _mockLogger = new Mock<ILogger<CompanyService>>();

        _service = new CompanyService(
            _mockSecEdgarService.Object,
            _mockConfiguration.Object,
            _mockRepository.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithValidFilter_ReturnsCorrectFundable()
    {
        var filter = new ForaFinCompaniesInputDto("A");
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Apple Inc",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Year = 2018, Income = 10000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2019, Income = 11000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2020, Income = 12000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2021, Income = 13000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2022, Income = 14000000000 }
                }
            }
        };
        _mockRepository.Setup(r => r.GetAllAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(filter, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Apple Inc", result[0].Name);
        // Calculate fundable
        var highestIncome = 14_000_000_000M; //decimal tenBillion = 10_000_000_000M;
        var standardFundable = 0.1233M * highestIncome;
        var specialFundable = standardFundable;
        specialFundable += 0.15M * specialFundable;// Name starts with vowel
        Assert.Equal(standardFundable, result[0].StandardFundableAmount);
        Assert.Equal(specialFundable, result[0].SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithValidFilter_ReturnsCorrectFundable_NoVocal()
    {
        var filter = new ForaFinCompaniesInputDto("X");
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Xiaomi Inc",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Year = 2018, Income = 10000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2019, Income = 11000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2020, Income = 12000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2021, Income = 14000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2022, Income = 13000000000 }
                }
            }
        };
        _mockRepository.Setup(r => r.GetAllAsync("X", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(filter, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Xiaomi Inc", result[0].Name);
        // Calculate fundable
        var highestIncome = 14_000_000_000M; //decimal tenBillion = 10_000_000_000M;
        var standardFundable = 0.1233M * highestIncome;
        var specialFundable = standardFundable;
        specialFundable -= 0.25M * specialFundable;// 2022 < 2021
        Assert.Equal(standardFundable, result[0].StandardFundableAmount);
        Assert.Equal(specialFundable, result[0].SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithValidFilter_ReturnsCorrectFundable_NoVocalAnd2022_GreaterThan_2021()
    {
        var filter = new ForaFinCompaniesInputDto("X");
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Xiaomi Inc",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Year = 2018, Income = 10000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2019, Income = 11000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2020, Income = 12000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2021, Income = 13000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2022, Income = 14000000000 }
                }
            }
        };
        _mockRepository.Setup(r => r.GetAllAsync("X", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(filter, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Xiaomi Inc", result[0].Name);
        // Calculate fundable
        var highestIncome = 14_000_000_000M; //decimal tenBillion = 10_000_000_000M;
        var standardFundable = 0.1233M * highestIncome;
        var specialFundable = standardFundable;//no vocal and 2022 > 2021
        Assert.Equal(standardFundable, result[0].StandardFundableAmount);
        Assert.Equal(specialFundable, result[0].SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithValidFilter_ReturnsCorrectFundable_NoFullYears()
    {
        var filter = new ForaFinCompaniesInputDto("X");
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Xiaomi Inc",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Year = 2018, Income = 10000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2020, Income = 12000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2022, Income = 14000000000 }
                }
            }
        };
        _mockRepository.Setup(r => r.GetAllAsync("X", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(filter, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Xiaomi Inc", result[0].Name);
        // Calculate fundable
        var standardFundable = 0; // not all years present
        var specialFundable = standardFundable;//no vocal and 2022 > 2021
        Assert.Equal(standardFundable, result[0].StandardFundableAmount);
        Assert.Equal(specialFundable, result[0].SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithValidFilter_ReturnsCorrectFundable_NoFullYears_Vowel()
    {
        var filter = new ForaFinCompaniesInputDto("A");
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Apple Inc",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Year = 2018, Income = 10000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2019, Income = 11000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2021, Income = 13000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2022, Income = 14000000000 }
                }
            }
        };
        _mockRepository.Setup(r => r.GetAllAsync("A", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(filter, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Apple Inc", result[0].Name);
        // Calculate fundable
        var standardFundable = 0M; // not all years present
        var specialFundable = standardFundable;
        specialFundable += 0.15M * specialFundable;// Name starts with vowel
        Assert.Equal(standardFundable, result[0].StandardFundableAmount);
        Assert.Equal(specialFundable, result[0].SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithValidFilter_ReturnsCorrectFundable_NoFullYears_2022_LessThan_2021()
    {
        var filter = new ForaFinCompaniesInputDto("X");
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Xiaomi Inc",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Year = 2018, Income = 10000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2019, Income = 11000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2021, Income = 14000000000 },
                    new ForaFinCompanyIncomeInfo { Year = 2022, Income = 13000000000 }
                }
            }
        };
        _mockRepository.Setup(r => r.GetAllAsync("X", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(filter, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Xiaomi Inc", result[0].Name);
        // Calculate fundable
        var standardFundable = 0M; // not all years present
        var specialFundable = standardFundable;
        specialFundable -= 0.25M * specialFundable;// Name starts with x and 2022 < 2021
        Assert.Equal(standardFundable, result[0].StandardFundableAmount);
        Assert.Equal(specialFundable, result[0].SpecialFundableAmount);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_WithNullFilter_ReturnsAllCompanies()
    {
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany { Id = 1, Name = "Test Company", IncomeInfos = new List<ForaFinCompanyIncomeInfo>() }
        };
        _mockRepository.Setup(r => r.GetAllAsync("", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetCompanyFactsAsync(null, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetAllCikAsync_WithValidConfig_ReturnsCiks()
    {
        _mockConfiguration.Setup(c => c["SecEdgarCiks"]).Returns("123,456,789");

        var result = await _service.GetAllCikAsync();

        Assert.Equal(3, result.Length);
        Assert.Contains("123", result);
        Assert.Contains("456", result);
        Assert.Contains("789", result);
    }

    [Fact]
    public async Task GetAllCikAsync_WithEmptyConfig_ReturnsEmptyArray()
    {
        _mockConfiguration.Setup(c => c["SecEdgarCiks"]).Returns("");

        var result = await _service.GetAllCikAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task ImportCompaniesAsync_WithValidCiks()
    {
        _mockConfiguration.Setup(c => c["SecEdgarCiks"]).Returns("123");
        var edgarInfo = new EdgarCompanyInfo
        {
            EntityName = "Test Company",
            Facts = new EdgarCompanyInfo.InfoFact
            {
                UsGaap = new EdgarCompanyInfo.InfoFactUsGaap
                {
                    NetIncomeLoss = new EdgarCompanyInfo.InfoFactUsGaapNetIncomeLoss
                    {
                        Units = new EdgarCompanyInfo.InfoFactUsGaapIncomeLossUnits
                        {
                            Usd = (new List<EdgarCompanyInfo.InfoFactUsGaapIncomeLossUnitsUsd>
                            {
                                new EdgarCompanyInfo.InfoFactUsGaapIncomeLossUnitsUsd
                                {
                                    Form = "10-K",
                                    Frame = "CY2022",
                                    Val = 1000000
                                }
                            }).ToArray()
                        }
                    }
                }
            }
        };

        _mockSecEdgarService.Setup(s => s.GetCompanyFactsAsync("123", It.IsAny<CancellationToken>())).ReturnsAsync(edgarInfo);
        _mockRepository.Setup(r => r.AddRangeAsync(It.IsAny<IEnumerable<ForaFinCompany>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _service.ImportCompaniesAsync();

        Assert.Contains("Imported 1 out of 1 CIKs", result);
        _mockRepository.Verify(r => r.AddRangeAsync(It.IsAny<IEnumerable<ForaFinCompany>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllCompaniesAsync_ReturnsDtoCompanies()
    {
        var companies = new List<ForaFinCompany>
        {
            new ForaFinCompany
            {
                Id = 1,
                Name = "Test Company",
                IncomeInfos = new List<ForaFinCompanyIncomeInfo>
                {
                    new ForaFinCompanyIncomeInfo { Id = 1, Year = 2022, Income = 1000000, CompanyId = 1 }
                }
            }
        };

        _mockRepository.Setup(r => r.GetAllAsync("", It.IsAny<CancellationToken>())).ReturnsAsync(companies);

        var result = await _service.GetAllCompaniesAsync();

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
        Assert.Equal("Test Company", result[0].Name);
        Assert.Single(result[0].IncomeInfos);
    }
}