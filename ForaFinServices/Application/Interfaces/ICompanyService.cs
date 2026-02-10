using System.Security;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Domain;

namespace ForaFinServices.Application.Interfaces;
public interface ICompanyService
{
    Task<List<ForaFinCompaniesOutputDto>> GetCompanyFactsAsync(ForaFinCompaniesInputDto? Filter, CancellationToken ct = default);
    Task<string> ImportCompaniesAsync(CancellationToken ct = default);
    Task<List<ForaFinCompanyDto>> GetAllCompaniesAsync(CancellationToken ct = default);
    Task<string[]> GetAllCikAsync(CancellationToken ct = default);
}