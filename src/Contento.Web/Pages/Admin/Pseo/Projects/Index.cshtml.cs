using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo.Projects;

public class IndexModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly ICollectionService _collectionService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPseoProjectService projectService,
        ICollectionService collectionService,
        ILogger<IndexModel> logger)
    {
        _projectService = projectService;
        _collectionService = collectionService;
        _logger = logger;
    }

    public List<PseoProject> Projects { get; set; } = [];
    public Dictionary<Guid, int> ProjectPageCounts { get; set; } = [];
    public Dictionary<Guid, int> ProjectCollectionCounts { get; set; } = [];

    [BindProperty]
    public string ProjectName { get; set; } = string.Empty;

    [BindProperty]
    public string RootDomain { get; set; } = string.Empty;

    [BindProperty]
    public string Subdomain { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Projects = await _projectService.GetBySiteIdAsync(siteId);

            foreach (var project in Projects)
            {
                var collections = await _collectionService.GetByProjectIdAsync(project.Id);
                ProjectCollectionCounts[project.Id] = collections.Count;
                ProjectPageCounts[project.Id] = collections.Sum(c => c.PageCount);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load projects in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var fqdn = $"{Subdomain}.{RootDomain}";
            var project = new PseoProject
            {
                SiteId = siteId,
                Name = ProjectName,
                RootDomain = RootDomain,
                Subdomain = Subdomain,
                Fqdn = fqdn,
                Status = "pending_dns"
            };

            var created = await _projectService.CreateAsync(project);
            return RedirectToPage("Edit", new { id = created.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create project in {Page}", nameof(IndexModel));
            return RedirectToPage();
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _projectService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete project in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }
}
