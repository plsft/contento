using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo.Collections;

public class EditModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly ICollectionService _collectionService;
    private readonly IContentSchemaService _schemaService;
    private readonly INicheService _nicheService;
    private readonly IGenerationService _generationService;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IPseoProjectService projectService,
        ICollectionService collectionService,
        IContentSchemaService schemaService,
        INicheService nicheService,
        IGenerationService generationService,
        ILogger<EditModel> logger)
    {
        _projectService = projectService;
        _collectionService = collectionService;
        _schemaService = schemaService;
        _nicheService = nicheService;
        _generationService = generationService;
        _logger = logger;
    }

    // ─── Display data ────────────────────────────────
    public PseoCollection? Collection { get; set; }
    public List<PseoProject> Projects { get; set; } = [];
    public List<ContentSchema> Schemas { get; set; } = [];
    public List<NicheTaxonomy> AvailableNiches { get; set; } = [];
    public List<PseoCollectionNiche> SelectedNiches { get; set; } = [];
    public bool IsNew { get; set; }

    // ─── Form bindings ───────────────────────────────
    [BindProperty]
    public string CollectionName { get; set; } = string.Empty;

    [BindProperty]
    public Guid ProjectId { get; set; }

    [BindProperty]
    public Guid SchemaId { get; set; }

    [BindProperty]
    public string UrlPattern { get; set; } = string.Empty;

    [BindProperty]
    public string TitleTemplate { get; set; } = string.Empty;

    [BindProperty]
    public string? MetaDescTemplate { get; set; }

    [BindProperty]
    public string PublishSchedule { get; set; } = "manual";

    [BindProperty]
    public int BatchSize { get; set; } = 50;

    [BindProperty]
    public List<Guid> SelectedNicheIds { get; set; } = [];

    public async Task<IActionResult> OnGetAsync(Guid? id)
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            Projects = await _projectService.GetBySiteIdAsync(siteId);
            Schemas = await _schemaService.GetAllAsync();
            AvailableNiches = await _nicheService.GetAllSystemAsync();

            if (id.HasValue)
            {
                var collection = await _collectionService.GetByIdAsync(id.Value);
                if (collection == null) return RedirectToPage("Index");

                Collection = collection;
                CollectionName = collection.Name;
                ProjectId = collection.ProjectId;
                SchemaId = collection.SchemaId;
                UrlPattern = collection.UrlPattern;
                TitleTemplate = collection.TitleTemplate;
                MetaDescTemplate = collection.MetaDescTemplate;
                PublishSchedule = collection.PublishSchedule;
                BatchSize = collection.BatchSize;

                SelectedNiches = await _collectionService.GetNichesAsync(id.Value);
                SelectedNicheIds = SelectedNiches.Select(n => n.NicheId).ToList();

                // Add project-specific niches
                var projectNiches = await _nicheService.GetByProjectIdAsync(collection.ProjectId);
                AvailableNiches.AddRange(projectNiches);
            }
            else
            {
                IsNew = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load collection editor in {Page}", nameof(EditModel));
            return RedirectToPage("Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync(Guid? id)
    {
        try
        {
            PseoCollection collection;

            if (id.HasValue)
            {
                collection = await _collectionService.GetByIdAsync(id.Value) ?? new PseoCollection();
            }
            else
            {
                collection = new PseoCollection();
            }

            collection.Name = CollectionName;
            collection.ProjectId = ProjectId;
            collection.SchemaId = SchemaId;
            collection.UrlPattern = UrlPattern;
            collection.TitleTemplate = TitleTemplate;
            collection.MetaDescTemplate = MetaDescTemplate;
            collection.PublishSchedule = PublishSchedule;
            collection.BatchSize = BatchSize;

            if (id.HasValue)
            {
                await _collectionService.UpdateAsync(collection);
            }
            else
            {
                collection = await _collectionService.CreateAsync(collection);
                id = collection.Id;
            }

            // Update niche assignments
            var nicheAssignments = SelectedNicheIds.Select(nicheId => new PseoCollectionNiche
            {
                CollectionId = collection.Id,
                NicheId = nicheId
            }).ToList();

            await _collectionService.SetNichesAsync(collection.Id, nicheAssignments);

            return RedirectToPage(new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save collection in {Page}", nameof(EditModel));
            return RedirectToPage("Index");
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync(Guid id)
    {
        try
        {
            await _generationService.GenerateCollectionAsync(id, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start generation in {Page}", nameof(EditModel));
        }

        return RedirectToPage(new { id });
    }
}
