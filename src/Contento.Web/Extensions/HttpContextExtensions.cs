using Contento.Core.Models;

namespace Contento.Web.Extensions;

public static class HttpContextExtensions
{
    public static Site GetCurrentSite(this HttpContext ctx)
        => ctx.Items["CurrentSite"] as Site ?? throw new InvalidOperationException("Site not resolved. Is SiteResolutionMiddleware registered?");

    public static Guid GetCurrentSiteId(this HttpContext ctx)
        => ctx.GetCurrentSite().Id;
}
