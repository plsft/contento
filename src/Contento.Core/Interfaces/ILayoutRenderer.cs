namespace Contento.Core.Interfaces;

public class LayoutRenderContext
{
    public string? StructureJson { get; set; }
    public Dictionary<string, string> RegionContent { get; set; } = new();
    public string? MaxWidth { get; set; }
    public string? Gap { get; set; }
    public bool HasLayout { get; set; }
}

public interface ILayoutRenderer
{
    Task<LayoutRenderContext> BuildRenderContextAsync(Guid siteId, Guid? layoutId = null);
}
