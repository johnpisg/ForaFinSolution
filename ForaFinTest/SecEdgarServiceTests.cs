using ForaFin.CompaniesApi.Domain.External;
using ForaFin.CompaniesApi.Infrastructure.External;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace ForaFinTest;

public class SecEdgarServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<ILogger<SecEdgarService>> _mockLogger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SecEdgarService _service;

    public SecEdgarServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockLogger = new Mock<ILogger<SecEdgarService>>();
        _jsonOptions = new JsonSerializerOptions();

        _service = new SecEdgarService(
            _mockHttpClientFactory.Object,
            _jsonOptions,
            _mockLogger.Object
        );
    }

    // Método auxiliar para crear un HttpClient "fake" que Moq pueda controlar
    private HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ReturnsAsync(response);

        return new HttpClient(handlerMock.Object)
        {
            BaseAddress = new Uri("https://data.sec.gov/api/xbrl/companyfacts/")
        };
    }

    [Fact]
    public async Task GetCompanyFactsAsync_ValidResponse()
    {
        // Arrange
        var cik = "123";
        var expectedInfo = new EdgarCompanyInfo { EntityName = "Test Company" };
        var json = JsonSerializer.Serialize(expectedInfo);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json)
        };

        var httpClient = CreateMockHttpClient(mockResponse);
        _mockHttpClientFactory.Setup(f => f.CreateClient("ExternalApi")).Returns(httpClient);

        // Act
        var result = await _service.GetCompanyFactsAsync(cik, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Company", result.EntityName);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_NotFound_ReturnsNull()
    {
        // Arrange
        var cik = "123";
        var mockResponse = new HttpResponseMessage(HttpStatusCode.NotFound);

        var httpClient = CreateMockHttpClient(mockResponse);
        _mockHttpClientFactory.Setup(f => f.CreateClient("ExternalApi")).Returns(httpClient);

        // Act
        var result = await _service.GetCompanyFactsAsync(cik, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetCompanyFactsAsync_InvalidOperationException_WhenFails()
    {
        // Arrange
        var cik = "123";
        var handlerMock = new Mock<HttpMessageHandler>();

        // Forzamos una excepción en el envío
        handlerMock
           .Protected()
           .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.IsAny<HttpRequestMessage>(),
              ItExpr.IsAny<CancellationToken>()
           )
           .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri("https://api.test") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("ExternalApi")).Returns(httpClient);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.GetCompanyFactsAsync(cik, CancellationToken.None)
        );
    }
}