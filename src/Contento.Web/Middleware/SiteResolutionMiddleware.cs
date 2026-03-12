using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Middleware;

public class SiteResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SiteResolutionMiddleware> _logger;

    public SiteResolutionMiddleware(RequestDelegate next, ILogger<SiteResolutionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ISiteService siteService)
    {
        var host = context.Request.Host.Host;

        // Try domain-based resolution first
        var site = await siteService.GetByDomainAsync(host);

        if (site == null)
        {
            // Fallback to primary site
            var allSites = await siteService.GetAllAsync(page: 1, pageSize: 100);
            site = allSites.FirstOrDefault(s => s.IsPrimary) ?? allSites.FirstOrDefault();
        }

        if (site != null)
        {
            context.Items["CurrentSite"] = site;
        }
        else
        {
            _logger.LogWarning("No site resolved for host: {Host}", host);
        }

        await _next(context);
    }
}

public static class SiteResolutionMiddlewareExtensions
{
    public static IApplicationBuilder UseSiteResolution(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SiteResolutionMiddleware>();
    }
}
