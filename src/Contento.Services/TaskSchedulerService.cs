using System.Data;
using System.Text.Json;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing scheduled tasks with cron-like scheduling,
/// task execution, and run history logging.
/// </summary>
public class TaskSchedulerService : ITaskSchedulerService
{
    private readonly IDbConnection _db;
    private readonly ILogger<TaskSchedulerService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TaskSchedulerService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public TaskSchedulerService(IDbConnection db, ILogger<TaskSchedulerService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<ScheduledTask?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<ScheduledTask>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ScheduledTask>> GetAllAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        return await _db.QueryAsync<ScheduledTask>(
            "SELECT * FROM scheduled_tasks WHERE site_id = @SiteId ORDER BY name",
            new { SiteId = siteId });
    }

    /// <inheritdoc />
    public async Task<ScheduledTask> CreateAsync(ScheduledTask task)
    {
        Guard.Against.Null(task);
        Guard.Against.NullOrWhiteSpace(task.Name);
        Guard.Against.NullOrWhiteSpace(task.TaskType);

        task.Id = Guid.NewGuid();
        task.CreatedAt = DateTime.UtcNow;
        task.NextRunAt = GetNextCronTime(task.CronExpression, DateTime.UtcNow);

        await _db.InsertAsync(task);
        return task;
    }

    /// <inheritdoc />
    public async Task<ScheduledTask> UpdateAsync(ScheduledTask task)
    {
        Guard.Against.Null(task);
        Guard.Against.Default(task.Id);

        task.NextRunAt = GetNextCronTime(task.CronExpression, DateTime.UtcNow);

        await _db.UpdateAsync(task);
        return task;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var task = await _db.GetAsync<ScheduledTask>(id);
        if (task == null)
            return;

        await _db.DeleteAsync(task);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ScheduledTaskLog>> GetLogsAsync(Guid taskId, int page = 1, int pageSize = 50)
    {
        Guard.Against.Default(taskId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;

        return await _db.QueryAsync<ScheduledTaskLog>(
            "SELECT * FROM scheduled_task_logs WHERE task_id = @TaskId ORDER BY started_at DESC LIMIT @Limit OFFSET @Offset",
            new { TaskId = taskId, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ScheduledTask>> GetDueTasksAsync()
    {
        return await _db.QueryAsync<ScheduledTask>(
            "SELECT * FROM scheduled_tasks WHERE is_enabled = true AND next_run_at <= @Now",
            new { Now = DateTime.UtcNow });
    }

    /// <inheritdoc />
    public async Task<ScheduledTaskLog?> RunNowAsync(Guid taskId)
    {
        Guard.Against.Default(taskId);

        var task = await _db.GetAsync<ScheduledTask>(taskId);
        if (task == null)
            return null;

        await ExecuteTaskAsync(task);

        var logs = await _db.QueryAsync<ScheduledTaskLog>(
            "SELECT * FROM scheduled_task_logs WHERE task_id = @TaskId ORDER BY started_at DESC LIMIT 1",
            new { TaskId = taskId });

        return logs.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task ExecuteTaskAsync(ScheduledTask task)
    {
        Guard.Against.Null(task);

        var log = new ScheduledTaskLog
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            StartedAt = DateTime.UtcNow
        };

        try
        {
            switch (task.TaskType)
            {
                case "publish_scheduled":
                    await _db.ExecuteAsync(
                        "UPDATE posts SET status = 'published', published_at = @Now WHERE status = 'draft' AND scheduled_at IS NOT NULL AND scheduled_at <= @Now",
                        new { Now = DateTime.UtcNow });
                    break;

                case "cleanup_trash":
                    await _db.ExecuteAsync(
                        "DELETE FROM posts WHERE status = 'archived' AND updated_at < @Cutoff",
                        new { Cutoff = DateTime.UtcNow.AddDays(-30) });
                    break;

                case "cleanup_spam":
                    await _db.ExecuteAsync(
                        "DELETE FROM comments WHERE status = 'spam' AND created_at < @Cutoff",
                        new { Cutoff = DateTime.UtcNow.AddDays(-30) });
                    break;

                case "expire_memberships":
                    await _db.ExecuteAsync(
                        "UPDATE subscribers SET membership_tier = 'free', membership_plan_id = NULL WHERE membership_expires_at IS NOT NULL AND membership_expires_at < @Now AND membership_tier != 'free'",
                        new { Now = DateTime.UtcNow });
                    break;

                default:
                    _logger.LogWarning("Unknown task type: {TaskType}", task.TaskType);
                    break;
            }

            log.Status = "success";
            log.Message = $"Task {task.Name} completed";
            task.LastResult = "success";
            task.LastError = null;
        }
        catch (Exception ex)
        {
            log.Status = "failed";
            log.Message = ex.Message;
            task.LastResult = "failed";
            task.LastError = ex.Message;
        }
        finally
        {
            log.CompletedAt = DateTime.UtcNow;
            log.DurationMs = (int)(log.CompletedAt.Value - log.StartedAt).TotalMilliseconds;
            await _db.InsertAsync(log);
        }

        task.LastRunAt = DateTime.UtcNow;
        await _db.UpdateAsync(task);
        await ComputeNextRunAsync(task.Id);
    }

    /// <inheritdoc />
    public async Task ComputeNextRunAsync(Guid taskId)
    {
        Guard.Against.Default(taskId);

        var task = await _db.GetAsync<ScheduledTask>(taskId);
        if (task == null)
            return;

        var nextRun = GetNextCronTime(task.CronExpression, DateTime.UtcNow);

        await _db.ExecuteAsync(
            "UPDATE scheduled_tasks SET next_run_at = @NextRun WHERE id = @Id",
            new { NextRun = nextRun, Id = taskId });
    }

    /// <summary>
    /// Computes the next run time from a simplified cron expression.
    /// Supports: * * * * * (every minute), */N * * * * (every N minutes),
    /// 0 * * * * (hourly), M H * * * (daily at H:M), and falls back to 1 hour.
    /// </summary>
    private static DateTime? GetNextCronTime(string cronExpression, DateTime from)
    {
        if (string.IsNullOrWhiteSpace(cronExpression))
            return null;

        var parts = cronExpression.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5)
            return from.AddHours(1); // safe fallback

        var minutePart = parts[0];
        var hourPart = parts[1];

        // * * * * * → every minute
        if (minutePart == "*" && hourPart == "*")
            return from.AddMinutes(1);

        // */N * * * * → every N minutes
        if (minutePart.StartsWith("*/") && hourPart == "*")
        {
            if (int.TryParse(minutePart[2..], out var interval) && interval > 0)
                return from.AddMinutes(interval);
            return from.AddHours(1);
        }

        // 0 * * * * → every hour on the hour
        if (minutePart == "0" && hourPart == "*")
            return from.AddHours(1);

        // M H * * * → daily at H:M
        if (int.TryParse(minutePart, out var minute) && int.TryParse(hourPart, out var hour))
        {
            var candidate = new DateTime(from.Year, from.Month, from.Day, hour, minute, 0, DateTimeKind.Utc);
            if (candidate <= from)
                candidate = candidate.AddDays(1);
            return candidate;
        }

        // Default fallback
        return from.AddHours(1);
    }
}
