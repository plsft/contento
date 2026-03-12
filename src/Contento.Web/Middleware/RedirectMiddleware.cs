using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Middleware;

public class RedirectMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RedirectMiddleware> _logger;

    public RedirectMiddleware(RequestDelegate next, ILogger<RedirectMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRedirectService redirectService)
    {
        // Only handle GET requests
        if (!HttpMethods.IsGet(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // Skip internal paths
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_", StringComparison.Ordinal) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Resolve site
        var site = context.TryGetCurrentSite();
        if (site == null)
        {
            await _next(context);
            return;
        }

        // Normalize path to lowercase
        var normalizedPath = path.ToLowerInvariant();

        // Look up redirect
        var redirect = await redirectService.GetByFromPathAsync(site.Id, normalizedPath);
        if (redirect != null)
        {
            _logger.LogDebug("Redirecting {FromPath} -> {ToPath} ({StatusCode})",
                redirect.FromPath, redirect.ToPath, redirect.StatusCode);

            // Fire-and-forget hit count increment
            _ = Task.Run(async () =>
            {
                try
                {
                    await redirectService.IncrementHitCountAsync(redirect.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to increment hit count for redirect {RedirectId}", redirect.Id);
                }
            });

            context.Response.Redirect(redirect.ToPath, redirect.StatusCode == 301);
            return;
        }

        await _next(context);
    }
}

public static class RedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseRedirectMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RedirectMiddleware>();
    }
}
