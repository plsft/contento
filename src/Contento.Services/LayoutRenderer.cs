using System.Text.Json;
using Microsoft.Extensions.Logging;
using Noundry.Guardian;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

public class LayoutRenderer : ILayoutRenderer
{
    private readonly ILayoutService _layoutService;
    private readonly IComponentRendererRegistry _registry;
    private readonly IPluginRuntime? _pluginRuntime;
    private readonly ILogger<LayoutRenderer> _logger;

    public LayoutRenderer(
        ILayoutService layoutService,
        IComponentRendererRegistry registry,
        ILogger<LayoutRenderer> logger,
        IPluginRuntime? pluginRuntime = null)
    {
        _layoutService = Guard.Against.Null(layoutService);
        _registry = Guard.Against.Null(registry);
        _logger = Guard.Against.Null(logger);
        _pluginRuntime = pluginRuntime;
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
            var layoutContext = new LayoutComponentContext
            {
                ComponentId = component.Id,
                ContentType = component.ContentType,
                Content = component.Content,
                Settings = component.Settings,
                CssClasses = component.CssClasses,
                SortOrder = component.SortOrder
            };

            // 1. Try the registry
            var renderer = _registry.GetRenderer(component.ContentType);
            if (renderer != null)
                return renderer.Render(layoutContext);

            // 2. Try plugin hook
            if (_pluginRuntime != null)
            {
                var contextJson = JsonSerializer.Serialize(layoutContext);
                var results = _pluginRuntime.BroadcastHook("component:render", contextJson);
                var pluginResult = results.Values.FirstOrDefault(v => !string.IsNullOrEmpty(v));
                if (pluginResult != null)
                    return pluginResult;
            }

            // 3. Fallback
            return component.Content ?? "";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to render component {ComponentId}", component.Id);
            return "";
        }
    }
}
