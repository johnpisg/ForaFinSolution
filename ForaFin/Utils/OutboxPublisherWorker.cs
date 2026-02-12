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
            using var scope = _services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();
            var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();
            // Aquí inyectarías tu cliente de Azure Service Bus

            var importJobs = await context.ImportJobs
                .Where(m => (m.Status == "Pending" || m.Status == "Failed") && m.Tries < 3)
                .OrderBy(m => m.CreatedAt)
                .Take(20)
                .ToListAsync(stoppingToken);

            foreach (var job in importJobs)
            {
                try
                {
                    //obtener los ciks
                    var cikList = await companyService.GetAllCikAsync(stoppingToken);
                    var cikListToRequest = await companyService.GetNotStoredCiks(cikList.ToList(), stoppingToken);
                    var ready = true;
                    foreach(var cik in cikListToRequest)
                    {
                        try
                        {
                            var outbox = new OutboxMessage
                            {
                                Id = Guid.NewGuid(),
                                Type = "StartImport",
                                Content = cik,
                            };
                            context.OutboxMessages.Add(outbox);
                            await context.SaveChangesAsync(stoppingToken);
                            // SIMULACIÓN: Enviar StartImportRequestDto a la cola
                            var queueMessage = JsonSerializer.Serialize(new StartImportRequestDto(outbox.Id, cik));
                            _logger.LogInformation($"Publicando mensaje {queueMessage} a la cola...");
                        }
                        catch (Exception ex)
                        {
                            ready = false;
                            _logger.LogError(ex, $"Error publicando mensaje para CIK {cik}");
                        }
                    }
                    // Marcar como procesado
                    job.Tries += 1;
                    job.Status = ready ? "Processed" : "Failed";
                    await context.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publicando mensajes para el job {JobId}", job.Id);
                }
            }

            await Task.Delay(5000, stoppingToken); // Esperar 5 segundos para la siguiente vuelta
        }
    }
}