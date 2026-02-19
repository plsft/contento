namespace Contento.Core.Interfaces;

/// <summary>
/// Service for sending email notifications.
/// Handles comment notifications, reply notifications, and post-published alerts.
/// Uses configurable SMTP abstracted for future providers (SendGrid, Mailgun).
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to a post author when a new comment is posted.
    /// </summary>
    /// <param name="postId">The post that received the comment.</param>
    /// <param name="commentId">The new comment identifier.</param>
    Task NotifyNewCommentAsync(Guid postId, Guid commentId);

    /// <summary>
    /// Sends a notification to a parent comment's author when a reply is posted.
    /// </summary>
    /// <param name="parentCommentId">The parent comment identifier.</param>
    /// <param name="replyCommentId">The reply comment identifier.</param>
    Task NotifyCommentReplyAsync(Guid parentCommentId, Guid replyCommentId);

    /// <summary>
    /// Sends a notification when a post is published (for subscriber newsletters).
    /// </summary>
    /// <param name="postId">The published post identifier.</param>
    Task NotifyPostPublishedAsync(Guid postId);

    /// <summary>
    /// Sends a generic email notification using the specified template.
    /// </summary>
    /// <param name="to">Recipient email address.</param>
    /// <param name="subject">Email subject line.</param>
    /// <param name="templateName">The email template name (without extension).</param>
    /// <param name="templateData">Dictionary of template variable replacements.</param>
    Task SendEmailAsync(string to, string subject, string templateName, Dictionary<string, string> templateData);
}
