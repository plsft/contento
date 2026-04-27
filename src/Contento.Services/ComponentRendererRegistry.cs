using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// Collects all registered <see cref="IComponentRenderer"/> implementations via DI
/// and provides lookup by content type.
/// </summary>
public class ComponentRendererRegistry : IComponentRendererRegistry
{
    private readonly Dictionary<string, IComponentRenderer> _renderers;

    public ComponentRendererRegistry(IEnumerable<IComponentRenderer> renderers)
    {
        _renderers = new Dictionary<string, IComponentRenderer>(StringComparer.OrdinalIgnoreCase);
        foreach (var renderer in renderers)
        {
            _renderers[renderer.ContentType] = renderer;
        }
    }

    public IComponentRenderer? GetRenderer(string contentType)
    {
        return _renderers.TryGetValue(contentType, out var renderer) ? renderer : null;
    }

    public IReadOnlyList<string> GetRegisteredTypes()
    {
        return _renderers.Keys.OrderBy(k => k).ToList();
    }
}
