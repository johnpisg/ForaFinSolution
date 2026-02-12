
using System.Configuration;
using System.Text.Json;
using System.Text.RegularExpressions;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using ForaFinServices.Domain;
using ForaFinServices.Domain.Interfaces;
using ForaFinServices.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using static ForaFinServices.Domain.External.EdgarCompanyInfo;

namespace ForaFinServices.Infrastructure;
public class CompanyServiceSaga : CompanyService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<CompanyServiceSaga> _logger;
    public CompanyServiceSaga(ISecEdgarService secEdgarService, IConfiguration configuration,
                IForaFinRepository foraFinRepository, IBgTaskRepository bgTaskRepository, ILogger<CompanyServiceSaga> logger, IServiceScopeFactory serviceScopeFactory)
                :base(secEdgarService, configuration, foraFinRepository, bgTaskRepository, logger)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }
    public override async Task<ImportJob> StoreImportJobAsync(CancellationToken ct = default)
    {
        var jobId = Guid.NewGuid();
        using var scope = _serviceScopeFactory.CreateScope();
        var _context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Crear el registro de negocio
            var job = new ImportJob { Id = jobId, Status = "Pending" };
            _context.ImportJobs.Add(job);

            // Guardar ambos en un solo commit
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return job;
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public override async Task<string> ImportSingleCompanyAsync(StartImportRequestDto data, CancellationToken ct = default)
    {
        var outboxId = data.OutboxMessageId;
        var cik = data.Cik;
        var importResult = await base.ImportCompanyByCikAsync(cik, ct);
        if(importResult != null)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<ForaFinDb>();

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Companies.AddAsync(importResult, ct);
                var outboxMessage = await _context.OutboxMessages.FindAsync(outboxId);
                if(outboxMessage != null)
                {
                    outboxMessage.ProcessedAt = DateTime.UtcNow;
                    _context.OutboxMessages.Update(outboxMessage);
                }
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw new Exception($"Error saving imported company with CIK {cik}");
            }
        }
        return string.Empty;
    }
}