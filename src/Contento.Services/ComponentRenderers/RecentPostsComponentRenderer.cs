using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public class RecentPostsComponentRenderer : IComponentRenderer
{
    public string ContentType => "recent_posts";

    public string Render(LayoutComponentContext context)
    {
        var cssClasses = context.CssClasses ?? "";
        return $"""
            <div class="widget-recent-posts {cssClasses}" data-widget="recent_posts">
                <h3 class="text-lg font-semibold mb-3">Recent Posts</h3>
                <div data-region-widget="recent_posts"></div>
            </div>
            """;
    }
}
