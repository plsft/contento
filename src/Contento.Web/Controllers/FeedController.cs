using System.ServiceModel.Syndication;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

/// <summary>
/// RSS 2.0 and Atom feed generation for published posts.
/// </summary>
[AllowAnonymous]
[Tags("RSS Feeds")]
public class FeedController : Controller
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly ICategoryService _categoryService;

    public FeedController(IPostService postService, ISiteService siteService, ICategoryService categoryService)
    {
        _postService = postService;
        _siteService = siteService;
        _categoryService = categoryService;
    }

    [HttpGet("/feed.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get RSS feed")]
    [EndpointDescription("Generates an RSS 2.0 feed of the latest 50 published posts including full HTML content, tags, and publication dates. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Rss()
    {
        var site = HttpContext.TryGetCurrentSite();
        var siteName = site?.Name ?? "Contento";
        var siteTagline = site?.Tagline ?? "";
        var siteId = site?.Id ?? Guid.Empty;
        var locale = site?.Locale ?? "en";

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var posts = await _postService.GetAllAsync(siteId, status: "published", page: 1, pageSize: 50);

        return GenerateRssFeed(siteName, siteTagline, $"{baseUrl}/feed.xml", baseUrl, locale, posts);
    }

    [HttpGet("/category/{slug}/feed.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get category RSS feed")]
    [EndpointDescription("Generates an RSS 2.0 feed of the latest 50 published posts in the specified category. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CategoryRss(string slug)
    {
        var site = HttpContext.TryGetCurrentSite();
        var siteName = site?.Name ?? "Contento";
        var siteId = site?.Id ?? Guid.Empty;
        var locale = site?.Locale ?? "en";

        var category = await _categoryService.GetBySlugAsync(siteId, slug);
        if (category == null)
            return NotFound();

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var posts = await _postService.GetAllAsync(siteId, status: "published", categoryId: category.Id, page: 1, pageSize: 50);

        var feedTitle = $"{category.Name} \u2014 {siteName}";
        var feedDescription = category.Description ?? $"Posts in {category.Name}";

        return GenerateRssFeed(feedTitle, feedDescription, $"{baseUrl}/category/{slug}/feed.xml", baseUrl, locale, posts);
    }

    [HttpGet("/tag/{tag}/feed.xml")]
    [ResponseCache(Duration = 3600)]
    [EndpointSummary("Get tag RSS feed")]
    [EndpointDescription("Generates an RSS 2.0 feed of the latest 50 published posts with the specified tag. Cached for 1 hour.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> TagRss(string tag)
    {
        var site = HttpContext.TryGetCurrentSite();
        var siteName = site?.Name ?? "Contento";
        var siteId = site?.Id ?? Guid.Empty;
        var locale = site?.Locale ?? "en";

        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var posts = await _postService.GetAllAsync(siteId, status: "published", tag: tag, page: 1, pageSize: 50);

        var feedTitle = $"Posts tagged '{tag}' \u2014 {siteName}";
        var feedDescription = $"Posts tagged with '{tag}'";

        return GenerateRssFeed(feedTitle, feedDescription, $"{baseUrl}/tag/{tag}/feed.xml", baseUrl, locale, posts);
    }

    private IActionResult GenerateRssFeed(string title, string description, string feedUrl, string baseUrl, string locale, IEnumerable<Post> posts)
    {
        var feed = new SyndicationFeed(
            title,
            description,
            new Uri(baseUrl))
        {
            Language = locale,
            LastUpdatedTime = posts.Any()
                ? new DateTimeOffset(posts.Max(p => p.PublishedAt ?? p.CreatedAt))
                : DateTimeOffset.UtcNow,
            Copyright = new TextSyndicationContent($"Copyright {DateTime.UtcNow.Year} {title}"),
            Generator = "Contento CMS"
        };

        feed.Links.Add(SyndicationLink.CreateSelfLink(new Uri(feedUrl)));

        var items = new List<SyndicationItem>();
        foreach (var post in posts)
        {
            var item = new SyndicationItem(
                post.Title,
                new TextSyndicationContent(post.BodyHtml ?? post.Excerpt ?? "", TextSyndicationContentKind.Html),
                new Uri($"{baseUrl}/{post.Slug}"),
                post.Id.ToString(),
                new DateTimeOffset(post.UpdatedAt))
            {
                PublishDate = new DateTimeOffset(post.PublishedAt ?? post.CreatedAt)
            };

            if (post.Tags != null)
            {
                foreach (var tag in post.Tags)
                    item.Categories.Add(new SyndicationCategory(tag));
            }

            if (!string.IsNullOrEmpty(post.Excerpt))
                item.Summary = new TextSyndicationContent(post.Excerpt);

            items.Add(item);
        }

        feed.Items = items;

        using var ms = new MemoryStream();
        using (var writer = XmlWriter.Create(ms, new XmlWriterSettings
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            Async = true
        }))
        {
            var rssFormatter = new Rss20FeedFormatter(feed, false);
            rssFormatter.WriteTo(writer);
            writer.Flush();
        }

        return File(ms.ToArray(), "application/rss+xml; charset=utf-8");
    }
}
