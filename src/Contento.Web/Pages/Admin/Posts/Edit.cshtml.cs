using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Pages.Admin.Posts;

public class EditModel : PageModel
{
    private readonly IPostService _postService;
    private readonly IPostTypeService _postTypeService;
    private readonly IPostLockService _postLockService;

    public EditModel(IPostService postService, IPostTypeService postTypeService, IPostLockService postLockService)
    {
        _postService = postService;
        _postTypeService = postTypeService;
        _postLockService = postLockService;
    }

    public Post? Post { get; set; }
    public PostType? PostType { get; set; }
    public PostLockInfo? LockInfo { get; set; }
    public bool IsLocked { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return NotFound();

        Post = await _postService.GetByIdAsync(postId);
        if (Post == null)
            return NotFound();

        if (Post.PostTypeId.HasValue)
        {
            PostType = await _postTypeService.GetByIdAsync(Post.PostTypeId.Value);
        }

        // Post locking
        var userIdClaim = User.FindFirst("app_user_id")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            var userName = User.Identity?.Name ?? "Unknown";
            var existingLock = await _postLockService.GetLockAsync(postId);

            if (existingLock != null && existingLock.UserId != userId)
            {
                // Another user holds the lock
                IsLocked = true;
                LockInfo = existingLock;
            }
            else
            {
                // No lock or same user — acquire/refresh
                await _postLockService.AcquireLockAsync(postId, userId, userName);
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostReleaseLockAsync(string id)
    {
        if (!Guid.TryParse(id, out var postId))
            return NotFound();

        var userIdClaim = User.FindFirst("app_user_id")?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            await _postLockService.ReleaseLockAsync(postId, userId);
        }

        return RedirectToPage("/Admin/Posts/Index");
    }
}
