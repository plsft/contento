using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class CategoriesComponentRenderer : IComponentRenderer
{
    public string ContentType => "categories";

    public string Render(LayoutComponentContext context)
    {
        var cssClasses = context.CssClasses ?? "";
        return $"""
            <div class="widget-categories {cssClasses}" data-widget="categories">
                <h3 class="text-lg font-semibold mb-3">Categories</h3>
                <div data-region-widget="categories"></div>
            </div>
            """;
    }
}
