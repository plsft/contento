using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class MarkdownComponentRenderer : IComponentRenderer
{
    private readonly IMarkdownService _markdownService;

    public MarkdownComponentRenderer(IMarkdownService markdownService)
    {
        _markdownService = markdownService;
    }

    public string ContentType => "markdown";

    public string Render(LayoutComponentContext context)
    {
        return _markdownService.RenderToHtml(context.Content ?? "");
    }
}
