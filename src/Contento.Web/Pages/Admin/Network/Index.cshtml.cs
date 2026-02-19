using System.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Noundry.Tuxedo;

namespace Contento.Web.Pages.Admin.Network;

public class IndexModel : PageModel
{
    private readonly ISiteService _siteService;
    private readonly IPostService _postService;
    private readonly IUserService _userService;
    private readonly IDbConnection _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ISiteService siteService, IPostService postService, IUserService userService, IDbConnection db, ILogger<IndexModel> logger)
    {
        _siteService = siteService;
        _postService = postService;
        _userService = userService;
        _db = db;
        _logger = logger;
    }

    public IEnumerable<SiteStats> Sites { get; set; } = [];
    public int TotalPosts { get; set; }
    public int TotalUsers { get; set; }
    public int TotalComments { get; set; }
    public int TotalMedia { get; set; }
    public IEnumerable<User> AllUsers { get; set; } = [];

    public class SiteStats
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Domain { get; set; }
        public bool IsPrimary { get; set; }
        public int PostCount { get; set; }
        public int PublishedCount { get; set; }
        public int CommentCount { get; set; }
        public int MediaCount { get; set; }
        public int PluginCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public async Task OnGetAsync()
    {
        try
        {
            Sites = await _db.QueryAsync<SiteStats>(
                """
                SELECT s.id, s.name, s.domain, s.is_primary AS IsPrimary, s.created_at AS CreatedAt,
                       (SELECT COUNT(*) FROM posts WHERE site_id = s.id) AS PostCount,
                       (SELECT COUNT(*) FROM posts WHERE site_id = s.id AND status = 'published') AS PublishedCount,
                       (SELECT COUNT(*) FROM comments c JOIN posts p ON c.post_id = p.id WHERE p.site_id = s.id) AS CommentCount,
                       (SELECT COUNT(*) FROM media WHERE site_id = s.id) AS MediaCount,
                       (SELECT COUNT(*) FROM installed_plugins WHERE site_id = s.id AND is_enabled = true) AS PluginCount
                FROM sites s
                ORDER BY s.is_primary DESC, s.name
                """
            );

            AllUsers = await _db.QueryAsync<User>(
                "SELECT * FROM users WHERE is_active = true ORDER BY display_name"
            );

            TotalPosts = Sites.Sum(s => s.PostCount);
            TotalComments = Sites.Sum(s => s.CommentCount);
            TotalMedia = Sites.Sum(s => s.MediaCount);
            TotalUsers = AllUsers.Count();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load network overview in {Page}", nameof(IndexModel));
        }
    }
}
