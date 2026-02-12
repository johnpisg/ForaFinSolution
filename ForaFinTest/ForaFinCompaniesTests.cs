using ForaFin;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Infrastructure.Security;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace ForaFinTest;

public class ForaFinCompaniesTests
{
    private readonly Mock<ITokenValidator> _mockTokenValidator;
    private readonly Mock<ICompanyService> _mockCompanyService;
    private readonly Mock<ILogger<ForaFinCompanies>> _mockLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly ForaFinCompanies _functions;
    private readonly ForaFinCompanies _functionsMokValidator;
    private readonly TokenValidator _validator;

    public ForaFinCompaniesTests()
    {
        _mockTokenValidator = new Mock<ITokenValidator>();
        _mockCompanyService = new Mock<ICompanyService>();
        _mockLogger = new Mock<ILogger<ForaFinCompanies>>();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["SecretKey"]).Returns("supersecretkey12345678901234567890supersecretkey12345678901234567890");
        _mockConfiguration.Setup(c => c["MyIssuer"]).Returns("TestIssuer");
        _mockConfiguration.Setup(c => c["MyAudience"]).Returns("TestAudience");
        _validator = new TokenValidator(_mockConfiguration.Object);

        _functions = new ForaFinCompanies(
            _validator,
            _mockCompanyService.Object,
            _jsonOptions,
            _mockLogger.Object
        );

        _functionsMokValidator = new ForaFinCompanies(
            _mockTokenValidator.Object,
            _mockCompanyService.Object,
            _jsonOptions,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task GetForaFinCompanies_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var validToken = await _validator.GenerateTokenAsync();
        var filter = new ForaFinCompaniesInputDto("A");
        var expectedResult = new List<ForaFinCompaniesOutputDto>
        {
            new ForaFinCompaniesOutputDto(1, "Apple Inc", 1000000, 1150000)
        };
        _mockCompanyService.Setup(c => c.GetCompanyFactsAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var mockRequest = CreateMockHttpRequest("POST", validToken, filter);

        // Act
        var result = await _functions.GetForaFinCompanies(mockRequest.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ForaFinCompaniesOutputDto>>>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetForaFinCompanies_WithInvalidToken_ReturnsUnauthorized()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        var mockRequest = CreateMockHttpRequest("POST", invalidToken, null);

        // Act
        var result = await _functions.GetForaFinCompanies(mockRequest.Object, CancellationToken.None);

        // Assert
        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.UnauthorizedHttpResult>(result);
    }

    [Fact]
    public async Task ImportCompanies_WithValidToken_ReturnsOkResult()
    {
        // Arrange
        var validToken = await _validator.GenerateTokenAsync();

        var expectedMessage = "Imported 5 out of 10 CIKs.";
        _mockCompanyService.Setup(c => c.ImportCompaniesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedMessage);

        var mockRequest = CreateMockHttpRequest("POST", validToken, null);

        // Act
        var result = await _functions.ImportCompanies(mockRequest.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>(result);
        Assert.Equal(expectedMessage, okResult.Value);
    }

    [Fact]
    public async Task GetAllCompanies_WithValidToken_ReturnsOkResult()
    {
        // Arrange
        var validToken = await _validator.GenerateTokenAsync();

        var expectedResult = new List<ForaFinCompanyDto>
        {
            new ForaFinCompanyDto(1, "Test Company", new List<ForaFinCompanyIncomeInfoDto>())
        };
        _mockCompanyService.Setup(c => c.GetAllCompaniesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        var mockRequest = CreateMockHttpRequest("GET", validToken, null);

        // Act
        var result = await _functions.GetAllCompanies(mockRequest.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<List<ForaFinCompanyDto>>>(result);
        Assert.Equal(expectedResult, okResult.Value);
    }

    [Fact]
    public async Task GetToken_ReturnsOkResult()
    {
        // Arrange
        var expectedToken = "generated.jwt.token";
        _mockTokenValidator.Setup(t => t.GenerateTokenAsync()).ReturnsAsync(expectedToken);

        // Usamos el helper para evitar el error de constructor
        var mockRequest = CreateMockHttpRequest("GET", string.Empty, null);

        // Act
        var result = await _functionsMokValidator.GetToken(mockRequest.Object, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<string>>(result);
        Assert.Equal(expectedToken, okResult.Value);
    }

    /// <summary>
    /// Crea un Mock de HttpRequestData inyectando el FunctionContext necesario para evitar el error de proxy.
    /// </summary>
    private Mock<HttpRequestData> CreateMockHttpRequest(string method, string token, object? body)
    {
        var mockContext = new Mock<FunctionContext>();
        var mockRequest = new Mock<HttpRequestData>(mockContext.Object);

        var httpHeaders = new HttpHeadersCollection();
        if (!string.IsNullOrEmpty(token))
        {
            httpHeaders.Add("Authorization", $"Bearer {token}");
        }

        mockRequest.Setup(r => r.Method).Returns(method);
        mockRequest.Setup(r => r.Headers).Returns(httpHeaders);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            mockRequest.Setup(r => r.Body).Returns(stream);
        }
        else
        {
            // Importante: No devolver null para evitar NullReferenceException en el Worker
            mockRequest.Setup(r => r.Body).Returns(new MemoryStream());
        }

        return mockRequest;
    }
}