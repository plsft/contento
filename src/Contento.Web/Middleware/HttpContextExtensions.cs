using Contento.Core.Models;

namespace Contento.Web.Middleware;

public static class HttpContextExtensions
{
    public static Site GetCurrentSite(this HttpContext ctx)
        => ctx.Items["CurrentSite"] as Site ?? throw new InvalidOperationException("Site not resolved. Ensure SiteResolutionMiddleware is registered.");

    public static Guid GetCurrentSiteId(this HttpContext ctx)
        => ctx.GetCurrentSite().Id;

    public static Site? TryGetCurrentSite(this HttpContext ctx)
        => ctx.Items["CurrentSite"] as Site;
}
