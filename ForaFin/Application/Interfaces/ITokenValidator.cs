using System.Security.Claims;

namespace ForaFin.CompaniesApi.Application.Interfaces;
public interface ITokenValidator
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    Task<string> GenerateTokenAsync();
}