using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Pseo.Niches;

public class IndexModel : PageModel
{
    private readonly INicheService _nicheService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        INicheService nicheService,
        ILogger<IndexModel> logger)
    {
        _nicheService = nicheService;
        _logger = logger;
    }

    public List<NicheTaxonomy> Niches { get; set; } = [];
    public List<string> Categories { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty]
    public string NicheName { get; set; } = string.Empty;

    [BindProperty]
    public string NicheSlug { get; set; } = string.Empty;

    [BindProperty]
    public string NicheCategory { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        try
        {
            Niches = await _nicheService.SearchAsync(SearchQuery, Category);
            Categories = Niches.Select(n => n.Category).Distinct().OrderBy(c => c).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load niches in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var niche = new NicheTaxonomy
            {
                Name = NicheName,
                Slug = NicheSlug,
                Category = NicheCategory,
                IsSystem = false
            };

            await _nicheService.CreateAsync(niche);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create niche in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostForkAsync(Guid nicheId, Guid projectId)
    {
        try
        {
            await _nicheService.ForkAsync(nicheId, projectId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fork niche in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _nicheService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete niche in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }
}
