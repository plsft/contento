using System.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// Email notification service using configurable SMTP.
/// Sends comment notifications, reply notifications, and post-published alerts.
/// Now delegates email sending to IEmailService and wires newsletter on publish.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IDbConnection _db;
    private readonly IPostService _postService;
    private readonly ICommentService _commentService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly INewsletterService? _newsletterService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IDbConnection db,
        IPostService postService,
        ICommentService commentService,
        IUserService userService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<NotificationService> logger,
        INewsletterService? newsletterService = null)
    {
        _db = db;
        _postService = postService;
        _commentService = commentService;
        _userService = userService;
        _emailService = emailService;
        _newsletterService = newsletterService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyNewCommentAsync(Guid postId, Guid commentId)
    {
        try
        {
            var post = await _postService.GetByIdAsync(postId);
            if (post == null) return;

            var author = await _userService.GetByIdAsync(post.AuthorId);
            if (author == null || string.IsNullOrWhiteSpace(author.Email)) return;

            var comment = await _commentService.GetByIdAsync(commentId);
            if (comment == null) return;

            var templateData = new Dictionary<string, string>
            {
                ["PostTitle"] = post.Title,
                ["PostSlug"] = post.Slug,
                ["CommentAuthor"] = comment.AuthorName,
                ["CommentBody"] = comment.BodyMarkdown.Length > 200
                    ? comment.BodyMarkdown[..200] + "..."
                    : comment.BodyMarkdown,
                ["RecipientName"] = author.DisplayName
            };

            await SendEmailAsync(
                author.Email,
                $"New comment on \"{post.Title}\"",
                "new-comment",
                templateData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send new comment notification for post {PostId}", postId);
        }
    }

    public async Task NotifyCommentReplyAsync(Guid parentCommentId, Guid replyCommentId)
    {
        try
        {
            var parentComment = await _commentService.GetByIdAsync(parentCommentId);
            if (parentComment == null || string.IsNullOrWhiteSpace(parentComment.AuthorEmail)) return;

            var reply = await _commentService.GetByIdAsync(replyCommentId);
            if (reply == null) return;

            var post = await _postService.GetByIdAsync(parentComment.PostId);
            if (post == null) return;

            var templateData = new Dictionary<string, string>
            {
                ["PostTitle"] = post.Title,
                ["PostSlug"] = post.Slug,
                ["ReplyAuthor"] = reply.AuthorName,
                ["ReplyBody"] = reply.BodyMarkdown.Length > 200
                    ? reply.BodyMarkdown[..200] + "..."
                    : reply.BodyMarkdown,
                ["RecipientName"] = parentComment.AuthorName,
                ["OriginalComment"] = parentComment.BodyMarkdown.Length > 100
                    ? parentComment.BodyMarkdown[..100] + "..."
                    : parentComment.BodyMarkdown
            };

            await SendEmailAsync(
                parentComment.AuthorEmail,
                $"Reply to your comment on \"{post.Title}\"",
                "comment-reply",
                templateData);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send reply notification for comment {ParentId}", parentCommentId);
        }
    }

    public async Task NotifyPostPublishedAsync(Guid postId)
    {
        try
        {
            var post = await _postService.GetByIdAsync(postId);
            if (post == null) return;

            _logger.LogInformation("Post published notification sent for: {Title} ({PostId})", post.Title, postId);

            // Send newsletter to subscribers if service is available
            if (_newsletterService != null)
            {
                var subscriberCount = await _newsletterService.GetSubscriberCountAsync(post.SiteId);
                if (subscriberCount > 0)
                {
                    _logger.LogInformation("Sending newsletter for post {PostId} to {Count} subscribers",
                        postId, subscriberCount);
                    await _newsletterService.SendCampaignAsync(post.SiteId, postId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send post published notification for {PostId}", postId);
        }
    }

    public async Task SendEmailAsync(string to, string subject, string templateName,
        Dictionary<string, string> templateData)
    {
        var smtpHost = _configuration["Smtp:Host"];
        var smtpPortStr = _configuration["Smtp:Port"];
        var smtpUsername = _configuration["Smtp:Username"];
        var smtpPassword = _configuration["Smtp:Password"];
        var fromAddress = _configuration["Smtp:From"] ?? "noreply@contento.local";
        var fromName = _configuration["Smtp:FromName"] ?? "Contento";

        // If SMTP is not configured, log and skip
        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogDebug("SMTP not configured, skipping email to {To}: {Subject}", to, subject);
            return;
        }

        var body = BuildEmailBody(templateName, templateData);
        var port = int.TryParse(smtpPortStr, out var p) ? p : 587;

        using var client = new SmtpClient(smtpHost, port);
        if (!string.IsNullOrWhiteSpace(smtpUsername))
        {
            client.Credentials = new NetworkCredential(smtpUsername, smtpPassword);
            client.EnableSsl = true;
        }

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        message.To.Add(to);

        try
        {
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To}: {Subject}", to, subject);
            throw;
        }
    }

    private static string BuildEmailBody(string templateName, Dictionary<string, string> templateData)
    {
        // Simple template rendering — replaces {{Key}} with values
        var template = templateName switch
        {
            "new-comment" => """
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto; padding: 24px;">
                    <h2 style="color: #1A1A1A; font-size: 20px; margin-bottom: 16px;">New Comment</h2>
                    <p style="color: #6B6B6B;">Hi {{RecipientName}},</p>
                    <p style="color: #6B6B6B;">{{CommentAuthor}} left a comment on your post <strong>"{{PostTitle}}"</strong>:</p>
                    <blockquote style="border-left: 3px solid #3D5A80; padding: 12px 16px; margin: 16px 0; background: #FAFAF8; color: #1A1A1A;">
                        {{CommentBody}}
                    </blockquote>
                    <p style="color: #6B6B6B; font-size: 14px; margin-top: 24px;">
                        — Contento
                    </p>
                </div>
                """,
            "comment-reply" => """
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto; padding: 24px;">
                    <h2 style="color: #1A1A1A; font-size: 20px; margin-bottom: 16px;">Reply to Your Comment</h2>
                    <p style="color: #6B6B6B;">Hi {{RecipientName}},</p>
                    <p style="color: #6B6B6B;">{{ReplyAuthor}} replied to your comment on <strong>"{{PostTitle}}"</strong>:</p>
                    <p style="color: #999; font-size: 13px; margin-bottom: 8px;">Your comment:</p>
                    <blockquote style="border-left: 3px solid #ccc; padding: 8px 16px; margin: 8px 0; background: #f5f5f5; color: #6B6B6B; font-size: 14px;">
                        {{OriginalComment}}
                    </blockquote>
                    <p style="color: #999; font-size: 13px; margin-bottom: 8px;">Reply:</p>
                    <blockquote style="border-left: 3px solid #3D5A80; padding: 12px 16px; margin: 8px 0; background: #FAFAF8; color: #1A1A1A;">
                        {{ReplyBody}}
                    </blockquote>
                    <p style="color: #6B6B6B; font-size: 14px; margin-top: 24px;">
                        — Contento
                    </p>
                </div>
                """,
            _ => """
                <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto; padding: 24px;">
                    <p style="color: #6B6B6B;">{{Body}}</p>
                    <p style="color: #6B6B6B; font-size: 14px; margin-top: 24px;">— Contento</p>
                </div>
                """
        };

        foreach (var (key, value) in templateData)
        {
            template = template.Replace($"{{{{{key}}}}}", System.Net.WebUtility.HtmlEncode(value));
        }

        return template;
    }
}
