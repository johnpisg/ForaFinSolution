using ForaFinServices.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ForaFinServices.Infrastructure.Security;

public class TokenValidator : ITokenValidator
{
    private readonly string SecretKey;
    private readonly string MyIssuer;
    private readonly string MyAudience;

    public TokenValidator(IConfiguration configuration)
    {
        SecretKey = configuration["SecretKey"]!;
        MyIssuer = configuration["MyIssuer"]!;
        MyAudience = configuration["MyAudience"]!;
    }
    public Task<string> GenerateTokenAsync()
    {
        var key = Encoding.ASCII.GetBytes(SecretKey);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim("name", "TestUser") }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = MyIssuer,
            Audience = MyAudience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Task.FromResult(tokenHandler.WriteToken(token));
    }

    public async Task<ClaimsPrincipal?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = MyIssuer,
                ValidateAudience = true,
                ValidAudience = MyAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // Elimina el margen de 5 min por defecto
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return await Task.FromResult(principal);
        }
        catch
        {
            return null;
        }
    }
}