using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Pseo.Schemas;

public class IndexModel : PageModel
{
    private readonly IContentSchemaService _schemaService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IContentSchemaService schemaService,
        ILogger<IndexModel> logger)
    {
        _schemaService = schemaService;
        _logger = logger;
    }

    public List<ContentSchema> Schemas { get; set; } = [];

    [BindProperty]
    public string SchemaName { get; set; } = string.Empty;

    [BindProperty]
    public string SchemaSlug { get; set; } = string.Empty;

    [BindProperty]
    public string? SchemaDescription { get; set; }

    [BindProperty]
    public string RendererSlug { get; set; } = string.Empty;

    [BindProperty]
    public string TitlePattern { get; set; } = string.Empty;

    public async Task OnGetAsync()
    {
        try
        {
            Schemas = await _schemaService.GetAllAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load schemas in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        try
        {
            var schema = new ContentSchema
            {
                Name = SchemaName,
                Slug = SchemaSlug,
                Description = SchemaDescription,
                RendererSlug = RendererSlug,
                TitlePattern = TitlePattern,
                IsSystem = false
            };

            await _schemaService.CreateAsync(schema);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create schema in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _schemaService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete schema in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }
}
