using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo.Pages;

public class IndexModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly ICollectionService _collectionService;
    private readonly IPseoPageService _pageService;
    private readonly IPublishService _publishService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPseoProjectService projectService,
        ICollectionService collectionService,
        IPseoPageService pageService,
        IPublishService publishService,
        ILogger<IndexModel> logger)
    {
        _projectService = projectService;
        _collectionService = collectionService;
        _pageService = pageService;
        _publishService = publishService;
        _logger = logger;
    }

    public List<PseoPage> PseoPages { get; set; } = [];
    public List<PseoCollection> Collections { get; set; } = [];
    public Dictionary<Guid, string> CollectionNames { get; set; } = [];
    public int TotalCount { get; set; }

    [BindProperty(SupportsGet = true)]
    public string Status { get; set; } = "all";

    [BindProperty(SupportsGet = true)]
    public Guid? CollectionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CurrentPage { get; set; } = 1;

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var pageSize = 25;

        try
        {
            // Load projects and collections for the filter dropdown
            var projects = await _projectService.GetBySiteIdAsync(siteId);
            foreach (var project in projects)
            {
                var cols = await _collectionService.GetByProjectIdAsync(project.Id);
                foreach (var col in cols)
                {
                    Collections.Add(col);
                    CollectionNames[col.Id] = col.Name;
                }
            }

            // Load pages based on filters
            if (CollectionId.HasValue)
            {
                var status = Status == "all" ? null : Status;
                PseoPages = await _pageService.GetByCollectionIdAsync(CollectionId.Value, status, CurrentPage, pageSize);
            }
            else if (projects.Any())
            {
                // Show pages from the first project by default
                var status = Status == "all" ? null : Status;
                PseoPages = await _pageService.GetByProjectIdAsync(projects.First().Id, status, CurrentPage, pageSize);
            }

            TotalCount = PseoPages.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pSEO pages in {PageName}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostPublishAsync(Guid pageId)
    {
        try
        {
            await _publishService.PublishPageAsync(pageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish page in {PageName}", nameof(IndexModel));
        }

        return RedirectToPage(new { Status, CollectionId, CurrentPage });
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid pageId)
    {
        try
        {
            await _pageService.DeleteAsync(pageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete page in {PageName}", nameof(IndexModel));
        }

        return RedirectToPage(new { Status, CollectionId, CurrentPage });
    }

    public async Task<IActionResult> OnPostBulkPublishAsync(List<Guid> selectedPageIds)
    {
        try
        {
            foreach (var id in selectedPageIds)
            {
                await _publishService.PublishPageAsync(id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk publish pages in {PageName}", nameof(IndexModel));
        }

        return RedirectToPage(new { Status, CollectionId, CurrentPage });
    }

    public async Task<IActionResult> OnPostBulkDeleteAsync(List<Guid> selectedPageIds)
    {
        try
        {
            foreach (var id in selectedPageIds)
            {
                await _pageService.DeleteAsync(id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to bulk delete pages in {PageName}", nameof(IndexModel));
        }

        return RedirectToPage(new { Status, CollectionId, CurrentPage });
    }
}
