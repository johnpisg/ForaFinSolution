using ForaFin.CompaniesApi;
using ForaFin.CompaniesApi.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Moq;

namespace ForaFinTest;

public class TokenValidatorTests
{
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly TokenValidator _validator;

    public TokenValidatorTests()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["SecretKey"]).Returns("supersecretkey12345678901234567890supersecretkey12345678901234567890");
        _mockConfiguration.Setup(c => c["MyIssuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["MyAudience"]).Returns("TestAudience");

        _validator = new TokenValidator(_mockConfiguration.Object);
    }

    [Fact]
    public async Task GenerateTokenAsync_ReturnsToken()
    {
        var token = await _validator.GenerateTokenAsync();
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token);
    }

    [Fact]
    public async Task ValidateTokenAsync_ReturnsTestUser()
    {
        var token = await _validator.GenerateTokenAsync();
        var principal = await _validator.ValidateTokenAsync(token);
        Assert.NotNull(principal);
        Assert.Equal("TestUser", principal.Claims.First(c => c.Type == "name").Value);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsNull()
    {
        var principal = await _validator.ValidateTokenAsync("invalid.token.here");
        Assert.Null(principal);
    }

}