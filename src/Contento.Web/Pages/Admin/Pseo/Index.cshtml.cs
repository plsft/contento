using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo;

public class IndexModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly ICollectionService _collectionService;
    private readonly IPseoPageService _pageService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPseoProjectService projectService,
        ICollectionService collectionService,
        IPseoPageService pageService,
        ILogger<IndexModel> logger)
    {
        _projectService = projectService;
        _collectionService = collectionService;
        _pageService = pageService;
        _logger = logger;
    }

    public List<PseoProject> Projects { get; set; } = [];
    public int TotalPages { get; set; }
    public int TotalPublished { get; set; }
    public int TotalCollections { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Projects = await _projectService.GetBySiteIdAsync(siteId);

            foreach (var project in Projects)
            {
                var collections = await _collectionService.GetByProjectIdAsync(project.Id);
                TotalCollections += collections.Count;

                foreach (var col in collections)
                {
                    TotalPages += col.PageCount;
                    TotalPublished += col.PublishedCount;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pSEO dashboard in {Page}", nameof(IndexModel));
        }
    }
}
