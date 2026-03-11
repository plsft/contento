using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class MenuComponentRenderer : IComponentRenderer
{
    public string ContentType => "menu";

    public string Render(LayoutComponentContext context)
    {
        var cssClasses = context.CssClasses ?? "";
        return $"""
            <div class="widget-menu {cssClasses}" data-widget="menu">
                <div data-region-widget="menu"></div>
            </div>
            """;
    }
}
