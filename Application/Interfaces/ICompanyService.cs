using System.Security;
using ForaFin.CompaniesApi.Application.Dtos;
using ForaFin.CompaniesApi.Domain;

namespace ForaFin.CompaniesApi.Application.Interfaces;
public interface ICompanyService
{
    Task<List<ForaFinCompaniesOutputDto>> GetCompanyFactsAsync(ForaFinCompaniesInputDto? Filter);
    Task<string> ImportCompaniesAsync(CancellationToken ct = default);
    Task<List<ForaFinCompanyDto>> GetAllCompaniesAsync();
    Task<string[]> GetAllCikAsync();
}