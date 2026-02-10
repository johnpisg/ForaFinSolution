using System.Security.Claims;

namespace ForaFinServices.Application.Interfaces;
public interface ITokenValidator
{
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token);
    Task<string> GenerateTokenAsync();
}