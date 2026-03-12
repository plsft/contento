using Contento.Core.Interfaces;

namespace Contento.Web.BackgroundServices;

public class TaskSchedulerBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TaskSchedulerBackgroundService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    public TaskSchedulerBackgroundService(IServiceScopeFactory scopeFactory, ILogger<TaskSchedulerBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskSchedulerBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scheduler = scope.ServiceProvider.GetRequiredService<ITaskSchedulerService>();

                var dueTasks = await scheduler.GetDueTasksAsync();
                foreach (var task in dueTasks)
                {
                    try
                    {
                        await scheduler.ExecuteTaskAsync(task);
                        _logger.LogInformation("Executed scheduled task: {TaskName} ({TaskType})", task.Name, task.TaskType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to execute scheduled task: {TaskName}", task.Name);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in task scheduler polling loop");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }
}
