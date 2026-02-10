using ForaFinApi.Utils;
using ForaFinServices.Application.Dtos;
using ForaFinServices.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ForaFinApi.Controllers;

[ApiController]
[Route("[controller]")]
public class BigProcessController : ControllerBase
{
    private readonly ILogger<BigProcessController> _logger;
    private readonly IBackgroundTaskQueue _taskQueue;
    private readonly ICompanyService _companyService;

    public BigProcessController(ILogger<BigProcessController> logger, IBackgroundTaskQueue taskQueue, ICompanyService companyService)
    {
        _taskQueue = taskQueue;
        _companyService = companyService;
        _logger = logger;
    }

    [HttpPost(Name = "PostBigProcess")]

    public async Task<IActionResult> Post([FromBody] ForaFinCompaniesInputDto request)
    {
        _logger.LogInformation("Recibida petición para proceso masivo.");
        var taskId = Guid.NewGuid();
        var newTask = new BgTask
        {
            Id = taskId,
            StartsWith = request.StartsWith ?? "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Status = BgTaskStatus.Created
        };

        // 1. Guardar en BD (Necesitas inyectar tu DbContext aquí)
        await _companyService.StoreBgTaskAsync(newTask);

        // 2. Encolamos el trabajo
        var success = _taskQueue.TryQueueBackgroundWorkItem(new BgWorkItem(taskId, request.StartsWith ?? ""));
        if (!success)
        {
            _logger.LogWarning("Sistema saturado. Cola llena.");
            // Devolvemos un 429 (Too Many Requests) o 503 (Service Unavailable)
            return StatusCode(StatusCodes.Status429TooManyRequests,
                "El sistema está procesando demasiadas solicitudes. Inténtalo más tarde.");
        }

        // Respondemos 202 Accepted
        return AcceptedAtAction(nameof(GetCompanyFacts), new { id = taskId }, newTask);
    }

    [HttpGet("GetCompanyFacts/{id}")]
    public async Task<IActionResult> GetCompanyFacts(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Recibida petición para obtener resultados del proceso masivo con Id: {TaskId}", id);
        var task = await _companyService.GetBgTaskAsync(id);
        if(task == null) {
            _logger.LogWarning("No se encontró tarea con Id: {TaskId}", id);
            return NotFound();
        }
        if(task.Status == BgTaskStatus.Completed)
        {
            _logger.LogInformation("Retrieving results for completed task with Id: {TaskId}", id);
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            using var vinculado = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken);
            ForaFinCompaniesInputDto input = new ForaFinCompaniesInputDto(task.StartsWith);
            var result = await _companyService.GetCompanyFactsAsync(input, vinculado.Token);
            _logger.LogInformation("GetForaFinCompanies completed successfully.");
            return Ok(new { task.Id, task.Status, task.Comments, Result = result });
        }

        _logger.LogInformation("Retrieving status for task with Id: {TaskId}", id);
        return Ok(new { task.Id, task.Status, task.Comments });
    }
}
