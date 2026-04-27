using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class HtmlComponentRenderer : IComponentRenderer
{
    public string ContentType => "html";

    public string Render(LayoutComponentContext context)
    {
        return context.Content ?? "";
    }
}
