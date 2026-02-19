using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

public class LayoutRenderer : ILayoutRenderer
{
    private readonly IDbConnection _db;
    private readonly ILayoutService _layoutService;
    private readonly IMarkdownService _markdownService;
    private readonly ILogger<LayoutRenderer> _logger;

    public LayoutRenderer(
        IDbConnection db,
        ILayoutService layoutService,
        IMarkdownService markdownService,
        ILogger<LayoutRenderer> logger)
    {
        _db = Guard.Against.Null(db);
        _layoutService = Guard.Against.Null(layoutService);
        _markdownService = Guard.Against.Null(markdownService);
        _logger = Guard.Against.Null(logger);
    }

    public async Task<LayoutRenderContext> BuildRenderContextAsync(Guid siteId, Guid? layoutId = null)
    {
        var context = new LayoutRenderContext();

        try
        {
            Layout? layout = null;

            if (layoutId.HasValue && layoutId.Value != Guid.Empty)
            {
                layout = await _layoutService.GetByIdAsync(layoutId.Value);
            }

            layout ??= await _layoutService.GetDefaultAsync(siteId);

            if (layout == null)
            {
                context.HasLayout = false;
                return context;
            }

            context.HasLayout = true;
            context.StructureJson = layout.Structure;

            // Parse structure for maxWidth and gap
            try
            {
                using var doc = JsonDocument.Parse(layout.Structure);
                if (doc.RootElement.TryGetProperty("maxWidth", out var mw))
                    context.MaxWidth = mw.GetString();
                if (doc.RootElement.TryGetProperty("gap", out var gap))
                    context.Gap = gap.GetString();
            }
            catch (JsonException)
            {
                // Default values if structure is malformed
            }

            context.MaxWidth ??= "1200px";
            context.Gap ??= "1.5rem";

            // Load and render components
            var result = await _layoutService.GetWithComponentsAsync(layout.Id);
            if (result == null) return context;

            var (_, components) = result.Value;
            var componentsByRegion = components
                .Where(c => c.IsVisible)
                .OrderBy(c => c.SortOrder)
                .GroupBy(c => c.Region);

            foreach (var group in componentsByRegion)
            {
                var html = string.Join("\n", group.Select(c => RenderComponent(c)));
                context.RegionContent[group.Key] = html;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build layout render context for site {SiteId}", siteId);
            context.HasLayout = false;
        }

        return context;
    }

    private string RenderComponent(LayoutComponent component)
    {
        try
        {
            return component.ContentType switch
            {
                "html" => component.Content ?? "",
                "markdown" => _markdownService.RenderToHtml(component.Content ?? ""),
                "recent_posts" => RenderRecentPostsWidget(component),
                "categories" => RenderCategoriesWidget(component),
                _ => component.Content ?? ""
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render component {ComponentId}", component.Id);
            return "";
        }
    }

    private string RenderRecentPostsWidget(LayoutComponent component)
    {
        // Widget rendered server-side — returns placeholder HTML for recent posts
        var cssClasses = component.CssClasses ?? "";
        return $"""
            <div class="widget-recent-posts {cssClasses}" data-widget="recent_posts">
                <h3 class="text-lg font-semibold mb-3">Recent Posts</h3>
                <div data-region-widget="recent_posts"></div>
            </div>
            """;
    }

    private string RenderCategoriesWidget(LayoutComponent component)
    {
        var cssClasses = component.CssClasses ?? "";
        return $"""
            <div class="widget-categories {cssClasses}" data-widget="categories">
                <h3 class="text-lg font-semibold mb-3">Categories</h3>
                <div data-region-widget="categories"></div>
            </div>
            """;
    }
}
