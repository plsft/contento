using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class TagsComponentRenderer : IComponentRenderer
{
    public string ContentType => "tags";

    public string Render(LayoutComponentContext context)
    {
        var cssClasses = context.CssClasses ?? "";
        return $"""
            <div class="widget-tags {cssClasses}" data-widget="tags">
                <h3 class="text-lg font-semibold mb-3">Tags</h3>
                <div data-region-widget="tags"></div>
            </div>
            """;
    }
}
