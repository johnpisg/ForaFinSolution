using System.Security;
using ForaFin.CompaniesApi.Application.Dtos;
using ForaFin.CompaniesApi.Domain;

namespace ForaFin.CompaniesApi.Application.Interfaces;
public interface ICompanyService
{
    Task<List<ForaFinCompaniesOutputDto>> GetCompanyFactsAsync(ForaFinCompaniesInputDto? Filter, CancellationToken ct = default);
    Task<string> ImportCompaniesAsync(CancellationToken ct = default);
    Task<List<ForaFinCompanyDto>> GetAllCompaniesAsync(CancellationToken ct = default);
    Task<string[]> GetAllCikAsync(CancellationToken ct = default);
}