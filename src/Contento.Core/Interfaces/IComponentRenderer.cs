namespace Contento.Core.Interfaces;

/// <summary>
/// Context passed to component renderers with all component data
/// </summary>
public class LayoutComponentContext
{
    public Guid ComponentId { get; set; }
    public string ContentType { get; set; } = "";
    public string? Content { get; set; }
    public string Settings { get; set; } = "{}";
    public string? CssClasses { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Renders a specific content type within a layout component
/// </summary>
public interface IComponentRenderer
{
    string ContentType { get; }
    string Render(LayoutComponentContext context);
}

/// <summary>
/// Registry for discovering and resolving component renderers by content type
/// </summary>
public interface IComponentRendererRegistry
{
    IComponentRenderer? GetRenderer(string contentType);
    IReadOnlyList<string> GetRegisteredTypes();
}
