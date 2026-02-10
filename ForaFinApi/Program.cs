
using System.Text.Json;
using System.Text.Json.Serialization;
using ForaFinApi.Utils;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain.Interfaces;
using ForaFinServices.Infrastructure;
using ForaFinServices.Infrastructure.Converter;
using ForaFinServices.Infrastructure.External;
using ForaFinServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddDbContext<ForaFinDb>(opt =>
            opt
            // .UseSqlServer("connString", sqlOptions => sqlOptions.EnableRetryOnFailure())
            .UseInMemoryDatabase("ForaFinDb")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        );
        builder.Services.AddScoped<IForaFinRepository, ForaFinRepository>();
        builder.Services.AddScoped<IBgTaskRepository, BgTaskRepository>();
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

        builder.Services.AddSingleton<IBackgroundTaskQueue>(new BackgroundTaskQueue(100));
        builder.Services.AddHostedService<BigProcessWorker>();

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();

    }
}