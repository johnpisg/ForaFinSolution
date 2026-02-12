using System.Text.Json;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ForaFin.Utils;
public class OutboxPublisherWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxPublisherWorker> _logger;

    public OutboxPublisherWorker(IServiceProvider services, ILogger<OutboxPublisherWorker> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await ImporterWorker.Import(_services, stoppingToken, _logger);

            await Task.Delay(5000, stoppingToken); // Esperar 5 segundos para la siguiente vuelta
        }
    }
}