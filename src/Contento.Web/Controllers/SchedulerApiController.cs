using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

[Tags("Scheduler")]
[ApiController]
[Route("api/v1/scheduler")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class SchedulerApiController : ControllerBase
{
    private readonly ITaskSchedulerService _schedulerService;
    private readonly ISiteService _siteService;

    public SchedulerApiController(ITaskSchedulerService schedulerService, ISiteService siteService)
    {
        _schedulerService = schedulerService;
        _siteService = siteService;
    }

    [HttpGet("tasks")]
    [EndpointSummary("List scheduled tasks")]
    [EndpointDescription("Returns all scheduled tasks for the current site, including their cron expressions and last execution status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var tasks = await _schedulerService.GetAllAsync(siteId);
        return Ok(new { data = tasks });
    }

    [HttpGet("tasks/{id}")]
    [EndpointSummary("Get a scheduled task by ID")]
    [EndpointDescription("Returns the full details of a specific scheduled task including its configuration and execution history.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid task ID." } });

        var task = await _schedulerService.GetByIdAsync(parsedId);
        if (task == null)
            return NotFound();

        return Ok(new { data = task });
    }

    [HttpPost("tasks")]
    [EndpointSummary("Create a scheduled task")]
    [EndpointDescription("Creates a new scheduled task with a cron expression defining its execution schedule and task-specific settings.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            var task = new ScheduledTask
            {
                SiteId = siteId,
                Name = request.Name ?? string.Empty,
                TaskType = request.TaskType ?? string.Empty,
                CronExpression = request.CronExpression ?? string.Empty,
                Settings = request.Settings ?? "{}"
            };

            var created = await _schedulerService.CreateAsync(task);
            return Ok(new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("tasks/{id}")]
    [EndpointSummary("Update a scheduled task")]
    [EndpointDescription("Updates an existing scheduled task's name, type, cron expression, or settings. Only provided fields are modified.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateTaskRequest request)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid task ID." } });

        var existing = await _schedulerService.GetByIdAsync(parsedId);
        if (existing == null)
            return NotFound();

        try
        {
            existing.Name = request.Name ?? existing.Name;
            existing.TaskType = request.TaskType ?? existing.TaskType;
            existing.CronExpression = request.CronExpression ?? existing.CronExpression;
            existing.Settings = request.Settings ?? existing.Settings;

            var updated = await _schedulerService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("tasks/{id}")]
    [EndpointSummary("Delete a scheduled task")]
    [EndpointDescription("Permanently deletes a scheduled task and its execution history.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid task ID." } });

        await _schedulerService.DeleteAsync(parsedId);
        return NoContent();
    }

    [HttpPost("tasks/{id}/run")]
    [EndpointSummary("Run a task immediately")]
    [EndpointDescription("Triggers immediate execution of a scheduled task outside its normal cron schedule. Returns the execution log.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RunNow(string id)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid task ID." } });

        var task = await _schedulerService.GetByIdAsync(parsedId);
        if (task == null)
            return NotFound();

        try
        {
            var log = await _schedulerService.RunNowAsync(parsedId);
            return Ok(new { data = log });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = new { code = "EXECUTION_FAILED", message = ex.Message } });
        }
    }

    [HttpGet("tasks/{id}/logs")]
    [EndpointSummary("Get task execution logs")]
    [EndpointDescription("Returns a paginated list of execution logs for a specific scheduled task, including status, duration, and error details.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetLogs(string id, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (!Guid.TryParse(id, out var parsedId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid task ID." } });

        var logs = await _schedulerService.GetLogsAsync(parsedId, page, pageSize);
        return Ok(new { data = logs, meta = new { page, pageSize } });
    }
}

public class CreateTaskRequest
{
    public string? Name { get; set; }
    public string? TaskType { get; set; }
    public string? CronExpression { get; set; }
    public string? Settings { get; set; }
}

public class UpdateTaskRequest
{
    public string? Name { get; set; }
    public string? TaskType { get; set; }
    public string? CronExpression { get; set; }
    public string? Settings { get; set; }
}
