using System.Text;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Middleware;

public class PseoMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PseoMiddleware> _logger;

    /// <summary>
    /// Cloudflare header for the real client IP (set when proxied through CF).
    /// </summary>
    private const string CfConnectingIpHeader = "CF-Connecting-IP";

    /// <summary>
    /// Cloudflare header for the visitor's country (ISO 3166-1 alpha-2).
    /// </summary>
    private const string CfIpCountryHeader = "CF-IPCountry";

    public PseoMiddleware(RequestDelegate next, ILogger<PseoMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Resolve the host — prefer X-Forwarded-Host (set by Cloudflare/reverse proxies),
        // fall back to the standard Host header.
        var host = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()
                   ?? context.Request.Host.Host;

        // Strip port if present (e.g., "example.com:443" → "example.com")
        if (host.Contains(':'))
            host = host.Split(':')[0];

        // Skip localhost and common CMS paths early
        if (host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";

        // Skip internal paths — these belong to the main CMS app
        if (path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/admin/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_", StringComparison.Ordinal) ||
            path.StartsWith("/health", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Look up the pSEO project by FQDN — check HttpContext.Items cache first
        PseoProject? project;
        if (context.Items.TryGetValue("PseoProject", out var cached) && cached is PseoProject cachedProject)
        {
            project = cachedProject;
        }
        else
        {
            var projectService = context.RequestServices.GetRequiredService<IPseoProjectService>();
            project = await projectService.GetByFqdnAsync(host);
        }

        if (project == null || project.Status != "active")
        {
            await _next(context);
            return;
        }

        // Cache the resolved project in HttpContext.Items to avoid duplicate DB queries
        context.Items["PseoProject"] = project;

        // Read Cloudflare headers for analytics/logging
        var clientIp = context.Request.Headers[CfConnectingIpHeader].FirstOrDefault()
                       ?? context.Connection.RemoteIpAddress?.ToString();
        var country = context.Request.Headers[CfIpCountryHeader].FirstOrDefault();

        // Store Cloudflare metadata in HttpContext.Items for downstream use
        if (!string.IsNullOrEmpty(clientIp))
            context.Items["ClientIp"] = clientIp;
        if (!string.IsNullOrEmpty(country))
            context.Items["ClientCountry"] = country;

        // Add debugging response header
        context.Response.Headers["X-Contento-Project"] = project.Id.ToString();

        _logger.LogDebug(
            "pSEO project resolved: {ProjectName} for host {Host} (client: {ClientIp}, country: {Country})",
            project.Name, host, clientIp ?? "unknown", country ?? "unknown");

        var trimmedPath = path.TrimStart('/');

        // Root — index page listing collections/pages
        if (string.IsNullOrEmpty(trimmedPath))
        {
            context.Request.Path = "/Pseo/Index";
            await _next(context);
            return;
        }

        // Sitemap
        if (trimmedPath.Equals("sitemap.xml", StringComparison.OrdinalIgnoreCase))
        {
            await ServeSitemapAsync(context, project);
            return;
        }

        // Robots.txt
        if (trimmedPath.Equals("robots.txt", StringComparison.OrdinalIgnoreCase))
        {
            await ServeRobotsAsync(context, project);
            return;
        }

        // Try to resolve a page by slug
        var pageService = context.RequestServices.GetRequiredService<IPseoPageService>();
        var page = await pageService.GetBySlugAsync(project.Id, trimmedPath);

        if (page != null && page.Status == "published")
        {
            context.Items["PseoPage"] = page;
            context.Request.Path = "/Pseo/Page";
            await _next(context);
            return;
        }

        _logger.LogDebug("pSEO page not found: {Slug} in project {ProjectId}", trimmedPath, project.Id);
        context.Response.StatusCode = 404;
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(Build404Html(project));
    }

    private async Task ServeSitemapAsync(HttpContext context, PseoProject project)
    {
        var pageService = context.RequestServices.GetRequiredService<IPseoPageService>();

        // Fetch all published pages (up to 50,000 per sitemap spec)
        var pages = await pageService.GetByProjectIdAsync(project.Id, "published", page: 1, pageSize: 50000);

        var scheme = context.Request.Scheme;
        var baseUrl = $"{scheme}://{project.Fqdn}";

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        // Index page
        sb.AppendLine("  <url>");
        sb.AppendLine($"    <loc>{baseUrl}/</loc>");
        sb.AppendLine($"    <lastmod>{project.UpdatedAt:yyyy-MM-dd}</lastmod>");
        sb.AppendLine("    <changefreq>daily</changefreq>");
        sb.AppendLine("    <priority>1.0</priority>");
        sb.AppendLine("  </url>");

        foreach (var page in pages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{baseUrl}/{page.Slug}</loc>");
            sb.AppendLine($"    <lastmod>{page.UpdatedAt:yyyy-MM-dd}</lastmod>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.8</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        context.Response.ContentType = "application/xml; charset=utf-8";
        context.Response.Headers["X-Robots-Tag"] = "noindex";
        await context.Response.WriteAsync(sb.ToString());
    }

    private async Task ServeRobotsAsync(HttpContext context, PseoProject project)
    {
        var scheme = context.Request.Scheme;
        var baseUrl = $"{scheme}://{project.Fqdn}";

        var robots = new StringBuilder();
        robots.AppendLine("User-agent: *");
        robots.AppendLine("Allow: /");
        robots.AppendLine();
        robots.AppendLine($"Sitemap: {baseUrl}/sitemap.xml");

        context.Response.ContentType = "text/plain; charset=utf-8";
        await context.Response.WriteAsync(robots.ToString());
    }

    private static string Build404Html(PseoProject project)
    {
        var encodedName = System.Net.WebUtility.HtmlEncode(project.Name);
        return $$"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1" />
                <title>Page Not Found — {{encodedName}}</title>
                <style>
                    body { font-family: system-ui, -apple-system, sans-serif; display: flex; align-items: center; justify-content: center; min-height: 100vh; margin: 0; background: #f9fafb; color: #374151; }
                    .container { text-align: center; padding: 2rem; }
                    h1 { font-size: 4rem; margin: 0; color: #9ca3af; }
                    p { font-size: 1.25rem; margin-top: 1rem; }
                    a { color: #2563eb; text-decoration: none; }
                    a:hover { text-decoration: underline; }
                </style>
            </head>
            <body>
                <div class="container">
                    <h1>404</h1>
                    <p>The page you're looking for doesn't exist.</p>
                    <p><a href="/">Back to {{encodedName}}</a></p>
                </div>
            </body>
            </html>
            """;
    }
}

public static class PseoMiddlewareExtensions
{
    public static IApplicationBuilder UsePseoMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<PseoMiddleware>();
    }
}
