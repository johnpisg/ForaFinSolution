using System.Text.Json;
using System.Text.Json.Serialization;
using ForaFin.CompaniesApi.Application.Interfaces;
using ForaFin.CompaniesApi.Domain.Interfaces;
using ForaFin.CompaniesApi.Infrastructure;
using ForaFin.CompaniesApi.Infrastructure.External;
using ForaFin.CompaniesApi.Infrastructure.Persistence;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddDbContext<ForaFinDb>(opt =>
    opt
    // .UseSqlServer("connString", sqlOptions => sqlOptions.EnableRetryOnFailure())
    .UseInMemoryDatabase("ForaFinDb")
    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
);
builder.Services.AddScoped<IForaFinRepository, ForaFinRepository>();
builder.Services.AddScoped<ISecEdgarService, SecEdgarService>();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Converters =
    {
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
        new PaddedIntConverter()
    }
});
builder.Services.AddHttpClient("ExternalApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration.GetValue<string>("SecEdgarApiBaseUrl") ?? "");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "PostmanRuntime/7.34.0");
    client.DefaultRequestHeaders.Add("Accept", "*/*");
});
builder.Services.AddProblemDetails();

var app = builder.Build();

app.Run();
