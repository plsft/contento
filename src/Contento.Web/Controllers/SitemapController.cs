using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// Dynamic XML sitemap generation with sitemap index support.
/// </summary>
[AllowAnonymous]
[Tags("Sitemap")]
public class SitemapController : Controller
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly ICategoryService _categoryService;
    private readonly ISeoService? _seoService;

    public SitemapController(IPostService postService, ISiteService siteService,
        ICategoryService categoryService, ISeoService? seoService = null)
    {
        _postService = postService;
        _siteService = siteService;
        _categoryService = categoryService;
        _seoService = seoService;
    }

    [HttpGet("/sitemap.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get sitemap index")]
    [EndpointDescription("Returns an XML sitemap index linking to posts, categories, tags, and pages sub-sitemaps. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Index()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (_seoService != null)
        {
            var xml = await _seoService.GenerateSitemapIndexAsync(siteId, baseUrl);
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        // Fallback to simple sitemap if SeoService not available
        return await PostsSitemap(1);
    }

    [HttpGet("/sitemap-posts-{page:int}.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get posts sitemap")]
    [EndpointDescription("Returns an XML sitemap containing URLs for all published posts with last-modified dates and change frequency. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PostsSitemap(int page = 1)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (_seoService != null)
        {
            var xml = await _seoService.GeneratePostSitemapAsync(siteId, baseUrl, page);
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        // Fallback
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<XElement>();
        urls.Add(CreateUrl(ns, baseUrl, DateTime.UtcNow, "daily", "1.0"));

        var posts = await _postService.GetAllAsync(siteId, status: "published", page: page, pageSize: 1000);
        foreach (var post in posts)
            urls.Add(CreateUrl(ns, $"{baseUrl}/{post.Slug}", post.UpdatedAt, "weekly", "0.8"));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset", urls));
        return Content(doc.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap-categories.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get categories sitemap")]
    [EndpointDescription("Returns an XML sitemap containing URLs for all category pages. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CategoriesSitemap()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (_seoService != null)
        {
            var xml = await _seoService.GenerateCategorySitemapAsync(siteId, baseUrl);
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var urls = new List<XElement>();
        var categories = await _categoryService.GetAllBySiteAsync(siteId);
        foreach (var cat in categories)
            urls.Add(CreateUrl(ns, $"{baseUrl}/category/{cat.Slug}", cat.CreatedAt, "weekly", "0.5"));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset", urls));
        return Content(doc.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap-tags.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get tags sitemap")]
    [EndpointDescription("Returns an XML sitemap containing URLs for all tag archive pages. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TagsSitemap()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (_seoService != null)
        {
            var xml = await _seoService.GenerateTagSitemapAsync(siteId, baseUrl);
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        // Fallback: empty urlset
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset"));
        return Content(doc.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/sitemap-pages.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get pages sitemap")]
    [EndpointDescription("Returns an XML sitemap containing URLs for all static pages. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> PagesSitemap()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        if (_seoService != null)
        {
            var xml = await _seoService.GeneratePageSitemapAsync(siteId, baseUrl);
            return Content(xml, "application/xml", Encoding.UTF8);
        }

        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(ns + "urlset"));
        return Content(doc.ToString(), "application/xml", Encoding.UTF8);
    }

    [HttpGet("/robots.txt")]
    [EndpointSummary("Get robots.txt")]
    [EndpointDescription("Returns the robots.txt file that instructs search engine crawlers which paths to index and provides the sitemap URL.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public IActionResult Robots()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var content = $"""
            User-agent: *
            Allow: /
            Disallow: /admin/
            Disallow: /api/

            Sitemap: {baseUrl}/sitemap.xml
            """;

        return Content(content, "text/plain", Encoding.UTF8);
    }

    [HttpGet("/llms.txt")]
    [EndpointSummary("Get llms.txt")]
    [EndpointDescription("Returns the llms.txt file that provides metadata about the site for large language model consumption.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public IActionResult LlmsTxt()
    {
        var site = HttpContext.TryGetCurrentSite();
        var siteName = site?.Name ?? "Contento";
        var siteTagline = site?.Tagline ?? "";

        var content = $"""
            # {siteName}
            > {siteTagline}

            This site is powered by Contento CMS.
            Content is authored by humans.
            """;

        return Content(content, "text/plain", Encoding.UTF8);
    }

    private static XElement CreateUrl(XNamespace ns, string loc, DateTime lastmod, string changefreq, string priority)
    {
        return new XElement(ns + "url",
            new XElement(ns + "loc", loc),
            new XElement(ns + "lastmod", lastmod.ToString("yyyy-MM-dd")),
            new XElement(ns + "changefreq", changefreq),
            new XElement(ns + "priority", priority));
    }
}
