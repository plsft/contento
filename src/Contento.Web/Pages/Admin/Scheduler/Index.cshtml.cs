using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Scheduler;

public class IndexModel : PageModel
{
    private readonly ITaskSchedulerService _taskSchedulerService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ITaskSchedulerService taskSchedulerService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _taskSchedulerService = taskSchedulerService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<ScheduledTask> Tasks { get; set; } = [];

    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public string TaskType { get; set; } = "";
    [BindProperty] public string CronExpression { get; set; } = "";

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        Tasks = await _taskSchedulerService.GetAllAsync(siteId);
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();
            var task = new ScheduledTask
            {
                SiteId = siteId,
                Name = Name,
                TaskType = TaskType,
                CronExpression = CronExpression
            };
            await _taskSchedulerService.CreateAsync(task);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create scheduled task");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRunNowAsync(Guid id)
    {
        try
        {
            await _taskSchedulerService.RunNowAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run task");
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _taskSchedulerService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete task");
        }
        return RedirectToPage();
    }
}
