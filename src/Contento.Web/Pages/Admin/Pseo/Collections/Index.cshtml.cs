using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo.Collections;

public class IndexModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly ICollectionService _collectionService;
    private readonly IContentSchemaService _schemaService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IPseoProjectService projectService,
        ICollectionService collectionService,
        IContentSchemaService schemaService,
        ILogger<IndexModel> logger)
    {
        _projectService = projectService;
        _collectionService = collectionService;
        _schemaService = schemaService;
        _logger = logger;
    }

    public List<PseoCollection> Collections { get; set; } = [];
    public Dictionary<Guid, string> SchemaNames { get; set; } = [];
    public Dictionary<Guid, string> ProjectNames { get; set; } = [];
    public Dictionary<Guid, int> CollectionNicheCounts { get; set; } = [];

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var projects = await _projectService.GetBySiteIdAsync(siteId);

            foreach (var project in projects)
            {
                ProjectNames[project.Id] = project.Name;
                var collections = await _collectionService.GetByProjectIdAsync(project.Id);

                foreach (var collection in collections)
                {
                    Collections.Add(collection);
                    var niches = await _collectionService.GetNichesAsync(collection.Id);
                    CollectionNicheCounts[collection.Id] = niches.Count;
                }
            }

            var schemas = await _schemaService.GetAllAsync();
            foreach (var schema in schemas)
            {
                SchemaNames[schema.Id] = schema.Name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load collections in {Page}", nameof(IndexModel));
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _collectionService.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete collection in {Page}", nameof(IndexModel));
        }

        return RedirectToPage();
    }
}
