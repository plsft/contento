using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Noundry.Tuxedo;

namespace Contento.Web.Pages.Admin.Posts;

public class HistoryModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IDbConnection _db;
    private readonly ILogger<HistoryModel> _logger;

    public HistoryModel(IPostService postService, IDbConnection db, ILogger<HistoryModel> logger)
    {
        _postService = postService;
        _db = db;
        _logger = logger;
    }

    public Post? CurrentPost { get; set; }
    public IEnumerable<PostVersion> Versions { get; set; } = [];

    [BindProperty(SupportsGet = true)]
    public int? CompareVersion { get; set; }

    public PostVersion? CompareVersionData { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return NotFound();

        try
        {
            CurrentPost = await _postService.GetByIdAsync(postId);
            if (CurrentPost == null)
                return NotFound();

            Versions = await _db.QueryAsync<PostVersion>(
                """
                SELECT id, post_id AS PostId, version, title, body_markdown AS BodyMarkdown,
                       body_html AS BodyHtml, change_summary AS ChangeSummary,
                       changed_by AS ChangedBy, created_at AS CreatedAt
                FROM post_versions
                WHERE post_id = @PostId
                ORDER BY version DESC
                """,
                new { PostId = postId }
            );

            if (CompareVersion.HasValue)
            {
                var compareResults = await _db.QueryAsync<PostVersion>(
                    """
                    SELECT id, post_id AS PostId, version, title, body_markdown AS BodyMarkdown,
                           body_html AS BodyHtml, change_summary AS ChangeSummary,
                           changed_by AS ChangedBy, created_at AS CreatedAt
                    FROM post_versions
                    WHERE post_id = @PostId AND version = @Version
                    LIMIT 1
                    """,
                    new { PostId = postId, Version = CompareVersion.Value }
                );
                CompareVersionData = compareResults.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load post history in {Page}", nameof(HistoryModel));
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRestoreAsync(string id, Guid versionId)
    {
        if (!Guid.TryParse(id, out var postId))
            return NotFound();

        try
        {
            var post = await _postService.GetByIdAsync(postId);
            if (post == null)
                return NotFound();

            var versionResults = await _db.QueryAsync<PostVersion>(
                """
                SELECT id, post_id AS PostId, version, title, body_markdown AS BodyMarkdown,
                       body_html AS BodyHtml, change_summary AS ChangeSummary,
                       changed_by AS ChangedBy, created_at AS CreatedAt
                FROM post_versions
                WHERE id = @VersionId AND post_id = @PostId
                LIMIT 1
                """,
                new { VersionId = versionId, PostId = postId }
            );

            var version = versionResults.FirstOrDefault();
            if (version == null)
                return NotFound();

            // Restore the post content from the version
            post.Title = version.Title ?? post.Title;
            post.BodyMarkdown = version.BodyMarkdown;
            post.BodyHtml = version.BodyHtml;

            // Determine the user performing the restore
            var userIdClaim = User.FindFirst("app_user_id")?.Value;
            var changedBy = Guid.TryParse(userIdClaim, out var uid) ? uid : post.AuthorId;

            await _postService.UpdateAsync(post, changedBy, $"Restored from version {version.Version}");

            _logger.LogInformation("Post {PostId} restored to version {Version} by user {UserId}",
                postId, version.Version, changedBy);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restore post {PostId} to version {VersionId}", postId, versionId);
        }

        return RedirectToPage(new { id });
    }
}
