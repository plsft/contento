using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Categories;

public class IndexModel : PageModel
{
    private readonly ICategoryService _categoryService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ICategoryService categoryService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _categoryService = categoryService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<Category> Categories { get; set; } = [];
    public int TotalCount { get; set; }

    [BindProperty]
    public string Name { get; set; } = string.Empty;

    [BindProperty]
    public string Slug { get; set; } = string.Empty;

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public Guid? ParentId { get; set; }

    [BindProperty]
    public Guid? EditId { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Categories = await _categoryService.GetTreeAsync(siteId);
            TotalCount = await _categoryService.GetTotalCountAsync(siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load categories in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var category = new Category
            {
                SiteId = siteId,
                Name = Name,
                Slug = string.IsNullOrWhiteSpace(Slug) ? GenerateSlug(Name) : Slug,
                Description = Description,
                ParentId = ParentId
            };
            await _categoryService.CreateAsync(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create category in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateAsync()
    {
        if (EditId == null) return RedirectToPage();

        try
        {
            var category = await _categoryService.GetByIdAsync(EditId.Value);
            if (category != null)
            {
                category.Name = Name;
                category.Slug = string.IsNullOrWhiteSpace(Slug) ? GenerateSlug(Name) : Slug;
                category.Description = Description;
                category.ParentId = ParentId;
                await _categoryService.UpdateAsync(category);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update category in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _categoryService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete category in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    private static string GenerateSlug(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("--", "-")
            .Trim('-');
    }
}
