using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages.Admin.Pseo;

public class OnboardingModel : PageModel
{
    private readonly IPseoProjectService _projectService;
    private readonly INicheService _nicheService;
    private readonly IContentSchemaService _schemaService;
    private readonly ICollectionService _collectionService;
    private readonly IGenerationService _generationService;
    private readonly ILogger<OnboardingModel> _logger;

    public OnboardingModel(
        IPseoProjectService projectService,
        INicheService nicheService,
        IContentSchemaService schemaService,
        ICollectionService collectionService,
        IGenerationService generationService,
        ILogger<OnboardingModel> logger)
    {
        _projectService = projectService;
        _nicheService = nicheService;
        _schemaService = schemaService;
        _collectionService = collectionService;
        _generationService = generationService;
        _logger = logger;
    }

    // ---- Display data ----
    public List<NicheTaxonomy> AvailableNiches { get; set; } = [];
    public List<ContentSchema> AvailableSchemas { get; set; } = [];
    public Dictionary<string, List<NicheTaxonomy>> NichesByCategory { get; set; } = new();

    // ---- Form bindings ----
    [BindProperty]
    public string ProjectName { get; set; } = string.Empty;

    [BindProperty]
    public string RootDomain { get; set; } = string.Empty;

    [BindProperty]
    public string SubdomainPrefix { get; set; } = "articles";

    [BindProperty]
    public string? HeaderHtml { get; set; }

    [BindProperty]
    public string? FooterHtml { get; set; }

    [BindProperty]
    public string? CustomCss { get; set; }

    [BindProperty]
    public List<Guid> SelectedNicheIds { get; set; } = [];

    [BindProperty]
    public List<Guid> SelectedSchemaIds { get; set; } = [];

    // ---- Result data (set after creation) ----
    public PseoProject? CreatedProject { get; set; }
    public Guid? CreatedCollectionId { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            AvailableNiches = await _nicheService.GetAllSystemAsync();
            AvailableSchemas = await _schemaService.GetAllAsync();

            // Group niches by category
            NichesByCategory = AvailableNiches
                .GroupBy(n => string.IsNullOrEmpty(n.Category) ? "Other" : n.Category)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Name).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load onboarding data");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();

            // 1. Create project
            var fqdn = $"{SubdomainPrefix}.{RootDomain}";
            var project = new PseoProject
            {
                SiteId = siteId,
                Name = ProjectName,
                RootDomain = RootDomain,
                Subdomain = SubdomainPrefix,
                Fqdn = fqdn,
                HeaderHtml = HeaderHtml,
                FooterHtml = FooterHtml,
                CustomCss = CustomCss
            };

            CreatedProject = await _projectService.CreateAsync(project);

            // 2. Create a collection for each selected schema
            foreach (var schemaId in SelectedSchemaIds)
            {
                var schema = await _schemaService.GetByIdAsync(schemaId);
                if (schema == null) continue;

                var collection = new PseoCollection
                {
                    ProjectId = CreatedProject.Id,
                    SchemaId = schemaId,
                    Name = $"{ProjectName} — {schema.Name}",
                    UrlPattern = "/{{niche-slug}}/{{subtopic-slug}}",
                    TitleTemplate = $"{{{{niche-name}}}} — {{{{subtopic}}}} {schema.Name}",
                    PublishSchedule = "manual",
                    BatchSize = 50,
                    Settings = "{}"
                };

                var created = await _collectionService.CreateAsync(collection);
                CreatedCollectionId ??= created.Id;

                // 3. Assign selected niches to the collection
                var nicheAssignments = SelectedNicheIds.Select(nicheId => new PseoCollectionNiche
                {
                    CollectionId = created.Id,
                    NicheId = nicheId
                }).ToList();

                await _collectionService.SetNichesAsync(created.Id, nicheAssignments);
            }

            // Reload niches and schemas for the view
            AvailableNiches = await _nicheService.GetAllSystemAsync();
            AvailableSchemas = await _schemaService.GetAllAsync();
            NichesByCategory = AvailableNiches
                .GroupBy(n => string.IsNullOrEmpty(n.Category) ? "Other" : n.Category)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Name).ToList());

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Onboarding wizard failed");
            ErrorMessage = "Something went wrong. Please try again.";

            // Reload data for re-rendering
            AvailableNiches = await _nicheService.GetAllSystemAsync();
            AvailableSchemas = await _schemaService.GetAllAsync();
            NichesByCategory = AvailableNiches
                .GroupBy(n => string.IsNullOrEmpty(n.Category) ? "Other" : n.Category)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Name).ToList());

            return Page();
        }
    }

    public async Task<IActionResult> OnPostGenerateAsync(Guid collectionId)
    {
        try
        {
            // Fire-and-forget generation
            _ = Task.Run(async () =>
            {
                try
                {
                    await _generationService.GenerateCollectionAsync(collectionId, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Background generation failed for collection {CollectionId}", collectionId);
                }
            });

            return new JsonResult(new { success = true, collectionId, message = "Generation started." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start generation for collection {CollectionId}", collectionId);
            return new JsonResult(new { success = false, error = ex.Message });
        }
    }
}
