using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class WidgetComponentRenderer : IComponentRenderer
{
    public string ContentType => "widget";

    public string Render(LayoutComponentContext context)
    {
        var cssClasses = context.CssClasses ?? "";
        return $"""
            <div class="widget-custom {cssClasses}" data-widget="custom">
                {context.Content ?? ""}
            </div>
            """;
    }
}
