using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.Controllers;

[Tags("Comments")]
[ApiController]
[Route("api/v1")]
public class CommentsApiController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IMarkdownService _markdownService;
    private readonly ISpamService _spamService;

    public CommentsApiController(ICommentService commentService, IMarkdownService markdownService, ISpamService spamService)
    {
        _commentService = commentService;
        _markdownService = markdownService;
        _spamService = spamService;
    }

    [HttpGet("posts/{postId}/comments")]
    [EndpointSummary("List comments for a post")]
    [EndpointDescription("Returns a threaded list of comments for the specified post, ordered hierarchically by parent-child relationships.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListByPost(string postId)
    {
        if (!Guid.TryParse(postId, out var pid))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid post ID." } });

        var comments = await _commentService.GetThreadedByPostAsync(pid);
        return Ok(new { data = comments });
    }

    [HttpPost("comments")]
    [AllowAnonymous]
    [EndpointSummary("Create a comment")]
    [EndpointDescription("Submits a new comment on a post. Includes honeypot spam detection and automatic spam scoring. Comments may be held for moderation.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateCommentRequest request)
    {
        try
        {
            // Honeypot check
            if (!string.IsNullOrEmpty(request.Website))
                return Ok(new { data = new { id = Guid.NewGuid() } });

            if (!Guid.TryParse(request.PostId, out var postId))
                return BadRequest(new { error = new { code = "INVALID_ID", message = "Post ID is required." } });

            var comment = new Comment
            {
                PostId = postId,
                ParentId = string.IsNullOrEmpty(request.ParentId) ? null : Guid.Parse(request.ParentId),
                AuthorName = request.AuthorName ?? "Anonymous",
                AuthorEmail = request.AuthorEmail,
                BodyMarkdown = request.BodyMarkdown ?? "",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = Request.Headers.UserAgent.ToString()
            };

            if (!string.IsNullOrWhiteSpace(comment.BodyMarkdown))
            {
                comment.BodyHtml = _markdownService.RenderCommentToHtml(comment.BodyMarkdown);
            }

            var created = await _commentService.CreateAsync(comment);

            // Run spam check and update comment status
            try
            {
                var spamResult = await _spamService.CheckCommentAsync(created);
                if (spamResult.Score >= 0.6m)
                    await _commentService.MarkSpamAsync(created.Id);
                else if (spamResult.Score < 0.3m)
                    await _commentService.ApproveAsync(created.Id);
                // 0.3-0.6 stays as "pending" for manual review
            }
            catch
            {
                // Spam check failure shouldn't block comment creation
            }

            return Ok(new { data = created });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = new { code = "VALIDATION_FAILED", message = ex.Message } });
        }
    }

    [HttpPost("comments/{id}/approve")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("Approve a comment")]
    [EndpointDescription("Approves a pending comment and trains the spam filter to recognize similar content as legitimate.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Approve(string id)
    {
        if (!Guid.TryParse(id, out var commentId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid comment ID." } });

        await _commentService.ApproveAsync(commentId);
        await _spamService.TrainHamAsync(commentId);
        return Ok(new { data = new { status = "approved" } });
    }

    [HttpPost("comments/{id}/spam")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("Mark comment as spam")]
    [EndpointDescription("Marks a comment as spam and trains the spam filter to recognize similar content in the future.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> MarkSpam(string id)
    {
        if (!Guid.TryParse(id, out var commentId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid comment ID." } });

        await _commentService.MarkSpamAsync(commentId);
        await _spamService.TrainSpamAsync(commentId);
        return Ok(new { data = new { status = "spam" } });
    }

    [HttpDelete("comments/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("Delete a comment")]
    [EndpointDescription("Permanently trashes a comment by its ID. Requires authentication.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var commentId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid comment ID." } });

        await _commentService.TrashAsync(commentId);
        return NoContent();
    }
}

public class CreateCommentRequest
{
    public string? PostId { get; set; }
    public string? AuthorName { get; set; }
    public string? AuthorEmail { get; set; }
    public string? BodyMarkdown { get; set; }
    public string? ParentId { get; set; }
    public string? Website { get; set; } // Honeypot field
}
