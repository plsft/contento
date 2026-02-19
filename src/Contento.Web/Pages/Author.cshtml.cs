using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;

namespace Contento.Web.Pages;

[AllowAnonymous]
public partial class AuthorModel : PageModel
{
    private readonly IPostService _postService;
    private readonly ISiteService _siteService;
    private readonly IUserService _userService;

    public AuthorModel(IPostService postService, ISiteService siteService, IUserService userService)
    {
        _postService = postService;
        _siteService = siteService;
        _userService = userService;
    }

    private const int PageSize = 10;

    public User? AuthorUser { get; set; }
    public string SiteName { get; set; } = "Contento";
    public string SiteTagline { get; set; } = "";
    public IEnumerable<Post> Posts { get; set; } = [];
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }

    public async Task<IActionResult> OnGetAsync(string slug, [FromQuery] int page = 1)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return NotFound();

        var site = HttpContext.GetCurrentSite();
        var siteId = site.Id;

        SiteName = site.Name;
        SiteTagline = site.Tagline ?? "";

        // Find the author by matching the slugified display name
        var users = await _userService.GetAllAsync(page: 1, pageSize: 500);
        AuthorUser = users.FirstOrDefault(u => u.IsActive && Slugify(u.DisplayName) == slug);

        if (AuthorUser == null)
            return NotFound();

        // Load all published posts and filter by this author
        var allPosts = await _postService.GetAllAsync(siteId, status: "published", page: 1, pageSize: 500);
        var authorPosts = allPosts.Where(p => p.AuthorId == AuthorUser.Id).OrderByDescending(p => p.PublishedAt).ToList();

        TotalCount = authorPosts.Count;
        TotalPages = (int)Math.Ceiling((double)TotalCount / PageSize);
        CurrentPage = Math.Clamp(page, 1, Math.Max(1, TotalPages));

        Posts = authorPosts.Skip((CurrentPage - 1) * PageSize).Take(PageSize);

        return Page();
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        // Normalize and remove diacritics
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        var result = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        result = SlugRegex().Replace(result, "");
        result = WhitespaceRegex().Replace(result, "-");
        result = result.Trim('-');
        return result;
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex SlugRegex();

    [GeneratedRegex(@"[\s-]+")]
    private static partial Regex WhitespaceRegex();
}
