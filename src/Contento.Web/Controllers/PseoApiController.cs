using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// REST API for programmatic SEO — projects, niches, schemas, collections, and pages
/// </summary>
[Tags("pSEO")]
[ApiController]
[Route("api/v1/pseo")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class PseoApiController : ControllerBase
{
    private readonly IPseoProjectService _projectService;
    private readonly INicheService _nicheService;
    private readonly IContentSchemaService _schemaService;
    private readonly ICollectionService _collectionService;
    private readonly IPseoPageService _pageService;
    private readonly IGenerationService _generationService;
    private readonly IPublishService _publishService;
    private readonly IPseoAnalyticsService _analyticsService;
    private readonly ILogger<PseoApiController> _logger;

    public PseoApiController(
        IPseoProjectService projectService,
        INicheService nicheService,
        IContentSchemaService schemaService,
        ICollectionService collectionService,
        IPseoPageService pageService,
        IGenerationService generationService,
        IPublishService publishService,
        IPseoAnalyticsService analyticsService,
        ILogger<PseoApiController> logger)
    {
        _projectService = projectService;
        _nicheService = nicheService;
        _schemaService = schemaService;
        _collectionService = collectionService;
        _pageService = pageService;
        _generationService = generationService;
        _publishService = publishService;
        _analyticsService = analyticsService;
        _logger = logger;
    }

    // ───────────────────────────────────────────────
    // Projects
    // ───────────────────────────────────────────────

    [HttpPost("projects")]
    [EndpointSummary("Create a pSEO project")]
    [EndpointDescription("Creates a new pSEO project with a subdomain under the given root domain. Automatically computes FQDN.")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest request)
    {
        try
        {
            var siteId = HttpContext.GetCurrentSiteId();
            var fqdn = $"{request.Subdomain}.{request.RootDomain}";

            var project = new PseoProject
            {
                SiteId = siteId,
                Name = request.Name ?? "Untitled Project",
                RootDomain = request.RootDomain ?? "",
                Subdomain = request.Subdomain ?? "",
                Fqdn = fqdn
            };

            var created = await _projectService.CreateAsync(project);
            return CreatedAtAction(nameof(GetProject), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpGet("projects")]
    [EndpointSummary("List pSEO projects")]
    [EndpointDescription("Returns all pSEO projects belonging to the current site.")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ListProjects()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var projects = await _projectService.GetBySiteIdAsync(siteId);
        return Ok(new { data = projects });
    }

    [HttpGet("projects/{id}")]
    [EndpointSummary("Get a pSEO project")]
    [EndpointDescription("Returns the full details of a pSEO project by ID.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetProject(string id)
    {
        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid project ID." } });

        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Project not found." } });

        return Ok(new { data = project });
    }

    [HttpPut("projects/{id}")]
    [EndpointSummary("Update a pSEO project")]
    [EndpointDescription("Updates an existing pSEO project's name, domain, or subdomain. Recomputes FQDN when domain fields change.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateProject(string id, [FromBody] UpdateProjectRequest request)
    {
        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid project ID." } });

        try
        {
            var existing = await _projectService.GetByIdAsync(projectId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Project not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.RootDomain != null) existing.RootDomain = request.RootDomain;
            if (request.Subdomain != null) existing.Subdomain = request.Subdomain;

            // Recompute FQDN if either domain field changed
            if (request.RootDomain != null || request.Subdomain != null)
                existing.Fqdn = $"{existing.Subdomain}.{existing.RootDomain}";

            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _projectService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("projects/{id}")]
    [EndpointSummary("Delete a pSEO project")]
    [EndpointDescription("Permanently deletes a pSEO project and all associated collections and pages.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteProject(string id)
    {
        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid project ID." } });

        var existing = await _projectService.GetByIdAsync(projectId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Project not found." } });

        await _projectService.DeleteAsync(projectId);
        return NoContent();
    }

    [HttpGet("projects/{id}/dns-status")]
    [EndpointSummary("Check DNS propagation")]
    [EndpointDescription("Checks whether the FQDN for a project has correct DNS records pointing to the platform.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CheckDnsStatus(string id)
    {
        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid project ID." } });

        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Project not found." } });

        var propagated = await _projectService.CheckDnsAsync(project.Fqdn);
        return Ok(new { data = new { fqdn = project.Fqdn, propagated, status = project.Status } });
    }

    [HttpPost("projects/{id}/verify")]
    [EndpointSummary("Re-verify domain")]
    [EndpointDescription("Re-checks DNS and updates project status to 'active' if the domain now resolves correctly.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> VerifyDomain(string id)
    {
        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid project ID." } });

        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Project not found." } });

        var propagated = await _projectService.CheckDnsAsync(project.Fqdn);
        if (propagated)
            await _projectService.UpdateStatusAsync(projectId, "active");

        var updated = await _projectService.GetByIdAsync(projectId);
        return Ok(new { data = updated, verified = propagated });
    }

    [HttpPut("projects/{id}/chrome")]
    [EndpointSummary("Update project chrome")]
    [EndpointDescription("Updates the shared header HTML, footer HTML, and custom CSS applied to all pages in the project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateChrome(string id, [FromBody] UpdateChromeRequest request)
    {
        if (!Guid.TryParse(id, out var projectId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid project ID." } });

        var project = await _projectService.GetByIdAsync(projectId);
        if (project == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Project not found." } });

        await _projectService.UpdateChromeAsync(projectId, request.HeaderHtml, request.FooterHtml, request.CustomCss);

        var updated = await _projectService.GetByIdAsync(projectId);
        return Ok(new { data = updated });
    }

    // ───────────────────────────────────────────────
    // Niches
    // ───────────────────────────────────────────────

    [HttpGet("niches")]
    [EndpointSummary("List niches")]
    [EndpointDescription("Returns niches filtered by optional query string and/or category. Returns both system and project niches.")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ListNiches(
        [FromQuery] string? query = null,
        [FromQuery] string? category = null)
    {
        var niches = await _nicheService.SearchAsync(query, category);
        return Ok(new { data = niches });
    }

    [HttpPost("niches")]
    [EndpointSummary("Create a custom niche")]
    [EndpointDescription("Creates a new project-specific niche taxonomy entry.")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateNiche([FromBody] CreateNicheRequest request)
    {
        try
        {
            var niche = new NicheTaxonomy
            {
                Name = request.Name ?? "Untitled Niche",
                Slug = request.Slug ?? "",
                Category = request.Category ?? "",
                Context = request.Context ?? "{}",
                IsSystem = false,
                ProjectId = request.ProjectId
            };

            var created = await _nicheService.CreateAsync(niche);
            return CreatedAtAction(nameof(ListNiches), null, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("niches/{id}")]
    [EndpointSummary("Update a niche")]
    [EndpointDescription("Updates an existing niche's name, slug, category, or context JSON.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateNiche(string id, [FromBody] UpdateNicheRequest request)
    {
        if (!Guid.TryParse(id, out var nicheId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid niche ID." } });

        try
        {
            var existing = await _nicheService.GetByIdAsync(nicheId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Niche not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.Slug != null) existing.Slug = request.Slug;
            if (request.Category != null) existing.Category = request.Category;
            if (request.Context != null) existing.Context = request.Context;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _nicheService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("niches/{id}")]
    [EndpointSummary("Delete a niche")]
    [EndpointDescription("Permanently deletes a niche taxonomy entry.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteNiche(string id)
    {
        if (!Guid.TryParse(id, out var nicheId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid niche ID." } });

        var existing = await _nicheService.GetByIdAsync(nicheId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Niche not found." } });

        await _nicheService.DeleteAsync(nicheId);
        return NoContent();
    }

    [HttpPost("niches/{id}/fork")]
    [EndpointSummary("Fork a system niche")]
    [EndpointDescription("Creates a project-specific copy of a system niche that can be customized independently.")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ForkNiche(string id, [FromBody] ForkNicheRequest request)
    {
        if (!Guid.TryParse(id, out var nicheId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid niche ID." } });

        try
        {
            var existing = await _nicheService.GetByIdAsync(nicheId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Niche not found." } });

            var forked = await _nicheService.ForkAsync(nicheId, request.ProjectId);
            return CreatedAtAction(nameof(ListNiches), null, new { data = forked });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    // ───────────────────────────────────────────────
    // Schemas
    // ───────────────────────────────────────────────

    [HttpGet("schemas")]
    [EndpointSummary("List content schemas")]
    [EndpointDescription("Returns all available content schemas (system and custom).")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ListSchemas()
    {
        var schemas = await _schemaService.GetAllAsync();
        return Ok(new { data = schemas });
    }

    [HttpGet("schemas/{id}")]
    [EndpointSummary("Get a content schema")]
    [EndpointDescription("Returns the full details of a content schema including its JSON definition and prompt templates.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetSchema(string id)
    {
        if (!Guid.TryParse(id, out var schemaId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid schema ID." } });

        var schema = await _schemaService.GetByIdAsync(schemaId);
        if (schema == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Schema not found." } });

        return Ok(new { data = schema });
    }

    [HttpPost("schemas")]
    [EndpointSummary("Create a content schema")]
    [EndpointDescription("Creates a new custom content schema with JSON structure definition and prompt templates.")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateSchema([FromBody] CreateSchemaRequest request)
    {
        try
        {
            var schema = new ContentSchema
            {
                Name = request.Name ?? "Untitled Schema",
                Slug = request.Slug ?? "",
                Description = request.Description,
                SchemaJson = request.SchemaJson ?? "{}",
                PromptTemplate = request.PromptTemplate ?? "",
                UserPromptTemplate = request.UserPromptTemplate ?? "",
                RendererSlug = request.RendererSlug ?? "",
                TitlePattern = request.TitlePattern ?? "",
                MetaDescPattern = request.MetaDescPattern,
                IsSystem = false,
                Settings = request.Settings ?? "{}"
            };

            var created = await _schemaService.CreateAsync(schema);
            return CreatedAtAction(nameof(GetSchema), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPut("schemas/{id}")]
    [EndpointSummary("Update a content schema")]
    [EndpointDescription("Updates an existing content schema's structure, prompts, or renderer configuration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateSchema(string id, [FromBody] UpdateSchemaRequest request)
    {
        if (!Guid.TryParse(id, out var schemaId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid schema ID." } });

        try
        {
            var existing = await _schemaService.GetByIdAsync(schemaId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Schema not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.Slug != null) existing.Slug = request.Slug;
            if (request.Description != null) existing.Description = request.Description;
            if (request.SchemaJson != null) existing.SchemaJson = request.SchemaJson;
            if (request.PromptTemplate != null) existing.PromptTemplate = request.PromptTemplate;
            if (request.UserPromptTemplate != null) existing.UserPromptTemplate = request.UserPromptTemplate;
            if (request.RendererSlug != null) existing.RendererSlug = request.RendererSlug;
            if (request.TitlePattern != null) existing.TitlePattern = request.TitlePattern;
            if (request.MetaDescPattern != null) existing.MetaDescPattern = request.MetaDescPattern;
            if (request.Settings != null) existing.Settings = request.Settings;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _schemaService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPost("schemas/{id}/validate")]
    [EndpointSummary("Validate content against a schema")]
    [EndpointDescription("Validates a JSON content string against the specified schema, returning validity status and any errors.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ValidateSchema(string id, [FromBody] ValidateSchemaRequest request)
    {
        if (!Guid.TryParse(id, out var schemaId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid schema ID." } });

        var schema = await _schemaService.GetByIdAsync(schemaId);
        if (schema == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Schema not found." } });

        var (isValid, errors) = await _schemaService.ValidateContentAsync(schemaId, request.ContentJson ?? "{}");
        return Ok(new { data = new { isValid, errors } });
    }

    // ───────────────────────────────────────────────
    // Collections
    // ───────────────────────────────────────────────

    [HttpPost("collections")]
    [EndpointSummary("Create a collection")]
    [EndpointDescription("Creates a new pSEO collection under a project with a schema, URL pattern, and title template.")]
    [ProducesResponseType(201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request)
    {
        try
        {
            var collection = new PseoCollection
            {
                ProjectId = request.ProjectId,
                SchemaId = request.SchemaId,
                Name = request.Name ?? "Untitled Collection",
                UrlPattern = request.UrlPattern ?? "",
                TitleTemplate = request.TitleTemplate ?? "",
                MetaDescTemplate = request.MetaDescTemplate,
                PublishSchedule = request.PublishSchedule ?? "manual",
                BatchSize = request.BatchSize ?? 50,
                Settings = request.Settings ?? "{}"
            };

            var created = await _collectionService.CreateAsync(collection);
            return CreatedAtAction(nameof(GetCollection), new { id = created.Id }, new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpGet("collections")]
    [EndpointSummary("List collections")]
    [EndpointDescription("Returns all collections, optionally filtered by project ID.")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ListCollections([FromQuery] Guid? projectId = null)
    {
        if (projectId == null)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId query parameter is required." } });

        var collections = await _collectionService.GetByProjectIdAsync(projectId.Value);
        return Ok(new { data = collections });
    }

    [HttpGet("collections/{id}")]
    [EndpointSummary("Get a collection")]
    [EndpointDescription("Returns the full details of a collection including its page counts and status.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCollection(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        return Ok(new { data = collection });
    }

    [HttpPut("collections/{id}")]
    [EndpointSummary("Update a collection")]
    [EndpointDescription("Updates an existing collection's name, templates, publish schedule, or settings.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateCollection(string id, [FromBody] UpdateCollectionRequest request)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        try
        {
            var existing = await _collectionService.GetByIdAsync(collectionId);
            if (existing == null)
                return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

            if (request.Name != null) existing.Name = request.Name;
            if (request.UrlPattern != null) existing.UrlPattern = request.UrlPattern;
            if (request.TitleTemplate != null) existing.TitleTemplate = request.TitleTemplate;
            if (request.MetaDescTemplate != null) existing.MetaDescTemplate = request.MetaDescTemplate;
            if (request.PublishSchedule != null) existing.PublishSchedule = request.PublishSchedule;
            if (request.BatchSize.HasValue) existing.BatchSize = request.BatchSize.Value;
            if (request.Settings != null) existing.Settings = request.Settings;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _collectionService.UpdateAsync(existing);
            return Ok(new { data = updated });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpDelete("collections/{id}")]
    [EndpointSummary("Delete a collection")]
    [EndpointDescription("Permanently deletes a collection and all its associated pages.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteCollection(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var existing = await _collectionService.GetByIdAsync(collectionId);
        if (existing == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        await _collectionService.DeleteAsync(collectionId);
        return NoContent();
    }

    [HttpPost("collections/{id}/generate")]
    [EndpointSummary("Trigger generation run")]
    [EndpointDescription("Starts AI content generation for all pending pages in a collection. Runs asynchronously.")]
    [ProducesResponseType(202)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GenerateCollection(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        // Fire-and-forget with a background scope; return 202 immediately
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

        return Accepted(new { data = new { collectionId, status = "generating", message = "Generation started." } });
    }

    [HttpGet("collections/{id}/progress")]
    [EndpointSummary("Get generation progress")]
    [EndpointDescription("Returns the current generation progress for a collection including counts of generated, failed, and pending pages.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCollectionProgress(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        var progress = await _generationService.GetProgressAsync(collectionId);
        return Ok(new { data = progress });
    }

    [HttpGet("collections/{id}/progress/stream")]
    [EndpointSummary("Stream generation progress via SSE")]
    [EndpointDescription("Returns a Server-Sent Events stream of generation progress for a collection, polling every 2 seconds until completed or failed.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task StreamCollectionProgress(Guid id, CancellationToken ct)
    {
        var collection = await _collectionService.GetByIdAsync(id);
        if (collection == null)
        {
            Response.StatusCode = 404;
            return;
        }

        Response.Headers["Content-Type"] = "text/event-stream";
        Response.Headers["Cache-Control"] = "no-cache";
        Response.Headers["Connection"] = "keep-alive";

        while (!ct.IsCancellationRequested)
        {
            var progress = await _generationService.GetProgressAsync(id);
            var json = JsonSerializer.Serialize(progress);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);

            if (progress.Status == "completed" || progress.Status == "failed" ||
                progress.Status == "generated" || progress.Status == "published")
                break;

            await Task.Delay(2000, ct);
        }
    }

    [HttpPost("collections/{id}/pause")]
    [EndpointSummary("Pause publishing")]
    [EndpointDescription("Pauses the automatic publishing pipeline for a collection.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PauseCollection(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        await _publishService.PausePublishingAsync(collectionId);
        var updated = await _collectionService.GetByIdAsync(collectionId);
        return Ok(new { data = updated });
    }

    [HttpPost("collections/{id}/resume")]
    [EndpointSummary("Resume publishing")]
    [EndpointDescription("Resumes the automatic publishing pipeline for a collection that was previously paused.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ResumeCollection(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        await _publishService.ResumePublishingAsync(collectionId);
        var updated = await _collectionService.GetByIdAsync(collectionId);
        return Ok(new { data = updated });
    }

    [HttpPost("collections/{id}/expand")]
    [EndpointSummary("Preview expanded subtopics")]
    [EndpointDescription("Dry-run that expands the collection's niches and subtopics into a preview list of pages that would be generated.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ExpandCollection(string id)
    {
        if (!Guid.TryParse(id, out var collectionId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid collection ID." } });

        var collection = await _collectionService.GetByIdAsync(collectionId);
        if (collection == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Collection not found." } });

        var pages = await _collectionService.ExpandSubtopicsAsync(collectionId);
        return Ok(new { data = pages, meta = new { totalPages = pages.Count } });
    }

    // ───────────────────────────────────────────────
    // Pages
    // ───────────────────────────────────────────────

    [HttpGet("pages")]
    [EndpointSummary("List pSEO pages")]
    [EndpointDescription("Returns a paginated list of pSEO pages, optionally filtered by collection, project, or status.")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ListPages(
        [FromQuery] Guid? collectionId = null,
        [FromQuery] Guid? projectId = null,
        [FromQuery] string? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        List<PseoPage> pages;

        if (collectionId.HasValue)
        {
            pages = await _pageService.GetByCollectionIdAsync(collectionId.Value, status, page, pageSize);
        }
        else if (projectId.HasValue)
        {
            pages = await _pageService.GetByProjectIdAsync(projectId.Value, status, page, pageSize);
        }
        else
        {
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "Either collectionId or projectId query parameter is required." } });
        }

        return Ok(new { data = pages, meta = new { page, pageSize } });
    }

    [HttpGet("pages/{id}")]
    [EndpointSummary("Get a pSEO page")]
    [EndpointDescription("Returns the full details of a pSEO page including its generated content, status, and metadata.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetPage(string id)
    {
        if (!Guid.TryParse(id, out var pageId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid page ID." } });

        var pseoPage = await _pageService.GetByIdAsync(pageId);
        if (pseoPage == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Page not found." } });

        return Ok(new { data = pseoPage });
    }

    [HttpPost("pages/{id}/regenerate")]
    [EndpointSummary("Regenerate a single page")]
    [EndpointDescription("Triggers AI content regeneration for a single page, replacing existing content.")]
    [ProducesResponseType(202)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RegeneratePage(string id)
    {
        if (!Guid.TryParse(id, out var pageId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid page ID." } });

        var pseoPage = await _pageService.GetByIdAsync(pageId);
        if (pseoPage == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Page not found." } });

        _ = Task.Run(async () =>
        {
            try
            {
                await _generationService.GenerateSinglePageAsync(pageId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background regeneration failed for page {PageId}", pageId);
            }
        });

        return Accepted(new { data = new { pageId, status = "regenerating", message = "Regeneration started." } });
    }

    [HttpPost("pages/{id}/publish")]
    [EndpointSummary("Publish a single page")]
    [EndpointDescription("Publishes a generated pSEO page, making it live on the project domain.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PublishPage(string id)
    {
        if (!Guid.TryParse(id, out var pageId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid page ID." } });

        var pseoPage = await _pageService.GetByIdAsync(pageId);
        if (pseoPage == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Page not found." } });

        await _publishService.PublishPageAsync(pageId);
        var updated = await _pageService.GetByIdAsync(pageId);
        return Ok(new { data = updated });
    }

    [HttpPost("pages/{id}/unpublish")]
    [EndpointSummary("Unpublish a page")]
    [EndpointDescription("Reverts a published pSEO page back to generated status, removing it from the live site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UnpublishPage(string id)
    {
        if (!Guid.TryParse(id, out var pageId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid page ID." } });

        var pseoPage = await _pageService.GetByIdAsync(pageId);
        if (pseoPage == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Page not found." } });

        await _publishService.UnpublishPageAsync(pageId);
        var updated = await _pageService.GetByIdAsync(pageId);
        return Ok(new { data = updated });
    }

    [HttpDelete("pages/{id}")]
    [EndpointSummary("Delete a pSEO page")]
    [EndpointDescription("Permanently deletes a pSEO page.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeletePage(string id)
    {
        if (!Guid.TryParse(id, out var pageId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid page ID." } });

        var pseoPage = await _pageService.GetByIdAsync(pageId);
        if (pseoPage == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "Page not found." } });

        await _pageService.DeleteAsync(pageId);
        return NoContent();
    }

    [HttpPost("pages/publish-batch")]
    [EndpointSummary("Publish a batch of pages")]
    [EndpointDescription("Publishes the next batch of generated pages within a collection, up to the specified batch size.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> PublishBatch([FromBody] PublishBatchRequest request)
    {
        try
        {
            await _publishService.PublishBatchAsync(request.CollectionId, request.BatchSize);
            var progress = await _generationService.GetProgressAsync(request.CollectionId);
            return Ok(new { data = progress, message = $"Batch publish completed for up to {request.BatchSize} pages." });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    // ───────────────────────────────────────────────
    // Analytics
    // ───────────────────────────────────────────────

    [HttpGet("analytics/summary")]
    [EndpointSummary("Get analytics summary")]
    [EndpointDescription("Returns aggregated search analytics for a pSEO project including clicks, impressions, CTR, and position.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetAnalyticsSummary([FromQuery] Guid projectId, [FromQuery] int days = 30)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        var summary = await _analyticsService.GetProjectSummaryAsync(projectId, days);
        return Ok(new { data = summary });
    }

    [HttpGet("analytics/pages")]
    [EndpointSummary("Get page-level analytics")]
    [EndpointDescription("Returns paginated analytics data for individual pages within a project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetPageAnalytics(
        [FromQuery] Guid projectId,
        [FromQuery] int days = 30,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        var pages = await _analyticsService.GetPageAnalyticsAsync(projectId, days, page, pageSize);
        return Ok(new { data = pages, meta = new { page, pageSize, days } });
    }

    [HttpGet("analytics/top-pages")]
    [EndpointSummary("Get top performing pages")]
    [EndpointDescription("Returns the top N pages by clicks for a project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetTopPages(
        [FromQuery] Guid projectId,
        [FromQuery] int days = 30,
        [FromQuery] int limit = 10)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        var pages = await _analyticsService.GetTopPagesAsync(projectId, days, limit);
        return Ok(new { data = pages });
    }

    [HttpGet("analytics/niches")]
    [EndpointSummary("Get niche performance")]
    [EndpointDescription("Returns aggregated search performance grouped by niche for a project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetNichePerformance([FromQuery] Guid projectId, [FromQuery] int days = 30)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        var niches = await _analyticsService.GetNichePerformanceAsync(projectId, days);
        return Ok(new { data = niches });
    }

    [HttpGet("analytics/zero-traffic")]
    [EndpointSummary("Get zero-traffic pages")]
    [EndpointDescription("Returns pages published for N+ days that have received zero clicks.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetZeroTrafficPages(
        [FromQuery] Guid projectId,
        [FromQuery] int daysSincePublish = 60)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        var pages = await _analyticsService.GetZeroTrafficPagesAsync(projectId, daysSincePublish);
        return Ok(new { data = pages, meta = new { daysSincePublish, count = pages.Count } });
    }

    [HttpGet("analytics/export")]
    [EndpointSummary("Export analytics CSV")]
    [EndpointDescription("Downloads a CSV file with page-level analytics data for a project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ExportAnalyticsCsv([FromQuery] Guid projectId, [FromQuery] int days = 30)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        var csv = await _analyticsService.ExportCsvAsync(projectId, days);
        return File(csv, "text/csv", $"pseo-analytics-{DateTime.UtcNow:yyyy-MM-dd}.csv");
    }

    [HttpPost("analytics/sync")]
    [EndpointSummary("Trigger manual GSC sync")]
    [EndpointDescription("Manually triggers a Google Search Console data sync for a project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SyncAnalytics([FromBody] SyncAnalyticsRequest request)
    {
        if (request.ProjectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        try
        {
            await _analyticsService.SyncGscDataAsync(request.ProjectId);
            return Ok(new { data = new { status = "synced", message = "GSC data synced successfully." } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual GSC sync failed for project {ProjectId}", request.ProjectId);
            return BadRequest(new { error = new { code = "SYNC_FAILED", message = ex.Message } });
        }
    }

    [HttpGet("analytics/gsc-auth-url")]
    [EndpointSummary("Get GSC auth URL")]
    [EndpointDescription("Returns the Google OAuth URL to initiate Search Console authorization for a project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GetGscAuthUrl([FromQuery] Guid projectId, [FromQuery] string redirectUri)
    {
        if (projectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        if (string.IsNullOrWhiteSpace(redirectUri))
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "redirectUri is required." } });

        try
        {
            var url = await _analyticsService.GetGscAuthUrlAsync(projectId, redirectUri);
            return Ok(new { data = new { authUrl = url } });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = new { code = "AUTH_FAILED", message = ex.Message } });
        }
    }

    [HttpPost("analytics/gsc-callback")]
    [EndpointSummary("Exchange GSC auth code")]
    [EndpointDescription("Exchanges the OAuth authorization code for access and refresh tokens and stores them on the project.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> GscCallback([FromBody] GscCallbackRequest request)
    {
        if (request.ProjectId == Guid.Empty)
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "projectId is required." } });

        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest(new { error = new { code = "MISSING_PARAM", message = "code is required." } });

        try
        {
            await _analyticsService.ExchangeGscCodeAsync(request.ProjectId, request.Code, request.RedirectUri ?? "");
            return Ok(new { data = new { status = "connected", message = "GSC connected successfully." } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GSC callback failed for project {ProjectId}", request.ProjectId);
            return BadRequest(new { error = new { code = "CALLBACK_FAILED", message = ex.Message } });
        }
    }

    // ───────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

// ───────────────────────────────────────────────
// Request DTOs
// ───────────────────────────────────────────────

// Projects
public class CreateProjectRequest
{
    public string? Name { get; set; }
    public string? RootDomain { get; set; }
    public string? Subdomain { get; set; }
}

public class UpdateProjectRequest
{
    public string? Name { get; set; }
    public string? RootDomain { get; set; }
    public string? Subdomain { get; set; }
}

public class UpdateChromeRequest
{
    public string? HeaderHtml { get; set; }
    public string? FooterHtml { get; set; }
    public string? CustomCss { get; set; }
}

// Niches
public class CreateNicheRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Category { get; set; }
    public string? Context { get; set; }
    public Guid? ProjectId { get; set; }
}

public class UpdateNicheRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Category { get; set; }
    public string? Context { get; set; }
}

public class ForkNicheRequest
{
    public Guid ProjectId { get; set; }
}

// Schemas
public class CreateSchemaRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? SchemaJson { get; set; }
    public string? PromptTemplate { get; set; }
    public string? UserPromptTemplate { get; set; }
    public string? RendererSlug { get; set; }
    public string? TitlePattern { get; set; }
    public string? MetaDescPattern { get; set; }
    public string? Settings { get; set; }
}

public class UpdateSchemaRequest
{
    public string? Name { get; set; }
    public string? Slug { get; set; }
    public string? Description { get; set; }
    public string? SchemaJson { get; set; }
    public string? PromptTemplate { get; set; }
    public string? UserPromptTemplate { get; set; }
    public string? RendererSlug { get; set; }
    public string? TitlePattern { get; set; }
    public string? MetaDescPattern { get; set; }
    public string? Settings { get; set; }
}

public class ValidateSchemaRequest
{
    public string? ContentJson { get; set; }
}

// Collections
public class CreateCollectionRequest
{
    public Guid ProjectId { get; set; }
    public Guid SchemaId { get; set; }
    public string? Name { get; set; }
    public string? UrlPattern { get; set; }
    public string? TitleTemplate { get; set; }
    public string? MetaDescTemplate { get; set; }
    public string? PublishSchedule { get; set; }
    public int? BatchSize { get; set; }
    public string? Settings { get; set; }
}

public class UpdateCollectionRequest
{
    public string? Name { get; set; }
    public string? UrlPattern { get; set; }
    public string? TitleTemplate { get; set; }
    public string? MetaDescTemplate { get; set; }
    public string? PublishSchedule { get; set; }
    public int? BatchSize { get; set; }
    public string? Settings { get; set; }
}

// Pages
public class PublishBatchRequest
{
    public Guid CollectionId { get; set; }
    public int BatchSize { get; set; } = 50;
}

// Analytics
public class SyncAnalyticsRequest
{
    public Guid ProjectId { get; set; }
}

public class GscCallbackRequest
{
    public Guid ProjectId { get; set; }
    public string? Code { get; set; }
    public string? RedirectUri { get; set; }
}
