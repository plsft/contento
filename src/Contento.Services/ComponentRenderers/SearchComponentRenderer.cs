using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class SearchComponentRenderer : IComponentRenderer
{
    public string ContentType => "search";

    public string Render(LayoutComponentContext context)
    {
        var cssClasses = context.CssClasses ?? "";
        return $"""
            <div class="widget-search {cssClasses}" data-widget="search">
                <div data-region-widget="search"></div>
            </div>
            """;
    }
}
