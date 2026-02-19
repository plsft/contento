using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class ContentPageModel : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly IMarkdownService _markdownService;

    public ContentPageModel(IPostService postService, ISiteService siteService, IMarkdownService markdownService)
    {
        _postService = postService;
        _siteService = siteService;
        _markdownService = markdownService;
    }

    public Post? PagePost { get; set; }
    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";

    public async Task<IActionResult> OnGetAsync(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var site = HttpContext.GetCurrentSite();
        var siteId = site.Id;

        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";
        var post = await _postService.GetBySlugAsync(siteId, slug);

        if (post == null || post.Status != "published")
            return NotFound();

        // If BodyHtml is empty but BodyMarkdown exists, render it
        if (string.IsNullOrEmpty(post.BodyHtml) && !string.IsNullOrEmpty(post.BodyMarkdown))
        {
            post.BodyHtml = _markdownService.RenderToHtml(post.BodyMarkdown);
        }

        PagePost = post;

        return Page();
    }
}
