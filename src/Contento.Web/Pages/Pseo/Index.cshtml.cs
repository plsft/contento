using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Pseo;

[AllowAnonymous]
public class PseoIndexModel : PageModel
{
    private readonly IPseoPageService _pageService;
    private readonly ICollectionService _collectionService;
    private readonly ILogger<PseoIndexModel> _logger;

    public PseoIndexModel(
        IPseoPageService pageService,
        ICollectionService collectionService,
        ILogger<PseoIndexModel> logger)
    {
        _pageService = pageService;
        _collectionService = collectionService;
        _logger = logger;
    }

    public PseoProject Project { get; set; } = null!;
    public List<PseoCollection> Collections { get; set; } = new();
    public List<PseoPage> Pages { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var project = HttpContext.Items["PseoProject"] as PseoProject;
        if (project == null)
            return NotFound();

        Project = project;

        // Load collections for this project
        Collections = await _collectionService.GetByProjectIdAsync(project.Id);

        // Load all published pages (up to 500 for listing)
        Pages = await _pageService.GetByProjectIdAsync(project.Id, "published", page: 1, pageSize: 500);

        return Page();
    }
}
