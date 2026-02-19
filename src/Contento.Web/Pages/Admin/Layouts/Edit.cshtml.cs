using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Layouts;

public class EditModel : PageModel
{
    private readonly ILayoutService _layoutService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(ILayoutService layoutService, ILogger<EditModel> logger)
    {
        _layoutService = layoutService;
        _logger = logger;
    }

    public Layout? LayoutItem { get; set; }
    public IEnumerable<LayoutComponent> Components { get; set; } = [];

    [BindProperty]
    public string Region { get; set; } = string.Empty;

    [BindProperty]
    public string ContentType { get; set; } = string.Empty;

    [BindProperty]
    public string? ComponentContent { get; set; }

    [BindProperty]
    public int SortOrder { get; set; }

    [BindProperty]
    public Guid ComponentId { get; set; }

    [BindProperty]
    public string? StructureJson { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return NotFound();

        try
        {
            var result = await _layoutService.GetWithComponentsAsync(layoutId);
            if (result == null)
                return NotFound();

            LayoutItem = result.Value.Layout;
            Components = result.Value.Components;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load layout in {Page}", nameof(EditModel));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddComponentAsync(string id)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return NotFound();

        try
        {
            var component = new LayoutComponent
            {
                LayoutId = layoutId,
                Region = Region,
                ContentType = ContentType,
                Content = ComponentContent,
                SortOrder = SortOrder
            };
            await _layoutService.AddComponentAsync(component);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add layout component in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateComponentAsync(string id)
    {
        if (!Guid.TryParse(id, out _))
            return NotFound();

        try
        {
            var component = new LayoutComponent
            {
                Id = ComponentId,
                LayoutId = Guid.Parse(id),
                Region = Region,
                ContentType = ContentType,
                Content = ComponentContent,
                SortOrder = SortOrder
            };
            await _layoutService.UpdateComponentAsync(component);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update layout component in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteComponentAsync(string id, Guid componentId)
    {
        try
        {
            await _layoutService.RemoveComponentAsync(componentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete layout component in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostSaveStructureAsync(string id)
    {
        if (!Guid.TryParse(id, out var layoutId))
            return NotFound();

        try
        {
            var result = await _layoutService.GetWithComponentsAsync(layoutId);
            if (result != null)
            {
                var layout = result.Value.Layout;
                layout.Structure = StructureJson ?? "{}";
                await _layoutService.UpdateAsync(layout);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save layout structure in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }
}
