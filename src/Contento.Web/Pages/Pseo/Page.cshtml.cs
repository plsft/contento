using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Pseo;

[AllowAnonymous]
public class PseoPageModel : PageModel
{
    private readonly IPseoRendererService _rendererService;
    private readonly ICollectionService _collectionService;
    private readonly IContentSchemaService _schemaService;
    private readonly ILogger<PseoPageModel> _logger;

    public PseoPageModel(
        IPseoRendererService rendererService,
        ICollectionService collectionService,
        IContentSchemaService schemaService,
        ILogger<PseoPageModel> logger)
    {
        _rendererService = rendererService;
        _collectionService = collectionService;
        _schemaService = schemaService;
        _logger = logger;
    }

    public string RenderedHtml { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        var project = HttpContext.Items["PseoProject"] as PseoProject;
        var page = HttpContext.Items["PseoPage"] as PseoPage;

        if (project == null || page == null)
            return NotFound();

        // If BodyHtml is already rendered, serve it directly
        if (!string.IsNullOrEmpty(page.BodyHtml))
        {
            RenderedHtml = page.BodyHtml;
            return Page();
        }

        // Otherwise, render on-the-fly from ContentJson using the schema's renderer
        if (!string.IsNullOrEmpty(page.ContentJson) && page.ContentJson != "{}")
        {
            try
            {
                var collection = await _collectionService.GetByIdAsync(page.CollectionId);
                if (collection == null)
                {
                    _logger.LogWarning("Collection {CollectionId} not found for page {PageId}", page.CollectionId, page.Id);
                    return NotFound();
                }

                var schema = await _schemaService.GetByIdAsync(collection.SchemaId);
                if (schema == null)
                {
                    _logger.LogWarning("Schema {SchemaId} not found for collection {CollectionId}", collection.SchemaId, collection.Id);
                    return NotFound();
                }

                RenderedHtml = await _rendererService.RenderPageAsync(page, project, schema);
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to render pSEO page {PageId} on-the-fly", page.Id);
                return StatusCode(500);
            }
        }

        _logger.LogWarning("pSEO page {PageId} has no BodyHtml or ContentJson", page.Id);
        return NotFound();
    }
}
