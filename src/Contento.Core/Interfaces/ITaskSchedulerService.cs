using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing scheduled tasks (cron-like task scheduler).
/// </summary>
public interface ITaskSchedulerService
{
    Task<ScheduledTask?> GetByIdAsync(Guid id);
    Task<IEnumerable<ScheduledTask>> GetAllAsync(Guid siteId);
    Task<ScheduledTask> CreateAsync(ScheduledTask task);
    Task<ScheduledTask> UpdateAsync(ScheduledTask task);
    Task DeleteAsync(Guid id);
    Task<ScheduledTaskLog?> RunNowAsync(Guid taskId);
    Task<IEnumerable<ScheduledTaskLog>> GetLogsAsync(Guid taskId, int page = 1, int pageSize = 50);
    Task ComputeNextRunAsync(Guid taskId);
    Task<IEnumerable<ScheduledTask>> GetDueTasksAsync();
    Task ExecuteTaskAsync(ScheduledTask task);
}
