namespace ForaFin.CompaniesApi.Application.Dtos;
public sealed record ForaFinCompaniesInputDto(string StartsWith);
public sealed record ForaFinCompaniesOutputDto(int Id, string Name, decimal StandardFundableAmount, decimal SpecialFundableAmount);

public sealed record ForaFinCompanyDto(int Id, string Name, List<ForaFinCompanyIncomeInfoDto> IncomeInfos);
public sealed record ForaFinCompanyIncomeInfoDto(int Id, int Year, decimal Income, int CompanyId);