using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

public class NewsletterService : INewsletterService
{
    private readonly IDbConnection _db;
    private readonly IEmailService _emailService;
    private readonly IPostService _postService;
    private readonly IMarkdownService _markdownService;
    private readonly ISiteService _siteService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<NewsletterService> _logger;

    public NewsletterService(
        IDbConnection db,
        IEmailService emailService,
        IPostService postService,
        IMarkdownService markdownService,
        ISiteService siteService,
        IConfiguration configuration,
        ILogger<NewsletterService> logger)
    {
        _db = Guard.Against.Null(db);
        _emailService = Guard.Against.Null(emailService);
        _postService = Guard.Against.Null(postService);
        _markdownService = Guard.Against.Null(markdownService);
        _siteService = Guard.Against.Null(siteService);
        _configuration = Guard.Against.Null(configuration);
        _logger = Guard.Against.Null(logger);
    }

    public async Task<Subscriber?> SubscribeAsync(Guid siteId, string email, string? displayName = null)
    {
        Guard.Against.Default(siteId);
        Guard.Against.NullOrWhiteSpace(email);

        email = email.ToLowerInvariant().Trim();

        // Check for existing subscriber
        var existing = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE site_id = @SiteId AND email = @Email",
            new { SiteId = siteId, Email = email });

        if (existing != null)
        {
            if (existing.Status == "unsubscribed")
            {
                existing.Status = "active";
                existing.UpdatedAt = DateTime.UtcNow;
                await _db.UpdateAsync(existing);
                _logger.LogInformation("Subscriber reactivated: {Email} for site {SiteId}", email, siteId);
            }
            return existing;
        }

        var subscriber = new Subscriber
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            Email = email,
            DisplayName = displayName,
            Status = "active",
            MembershipTier = "free",
            UnsubscribeToken = GenerateUnsubscribeToken(email, siteId),
            SubscribedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(subscriber);
        _logger.LogInformation("New subscriber: {Email} for site {SiteId}", email, siteId);
        return subscriber;
    }

    public async Task<bool> UnsubscribeAsync(string token)
    {
        Guard.Against.NullOrWhiteSpace(token);

        var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE unsubscribe_token = @Token",
            new { Token = token });

        if (subscriber == null) return false;

        subscriber.Status = "unsubscribed";
        subscriber.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(subscriber);

        _logger.LogInformation("Subscriber unsubscribed: {Email}", subscriber.Email);
        return true;
    }

    public async Task<IEnumerable<Subscriber>> GetActiveSubscribersAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);
        return await _db.QueryAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE site_id = @SiteId AND status = 'active' ORDER BY subscribed_at DESC",
            new { SiteId = siteId });
    }

    public async Task<int> GetSubscriberCountAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);
        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM subscribers WHERE site_id = @SiteId AND status = 'active'",
            new { SiteId = siteId });
    }

    public async Task<NewsletterCampaign> SendCampaignAsync(Guid siteId, Guid postId)
    {
        Guard.Against.Default(siteId);
        Guard.Against.Default(postId);

        var post = await _postService.GetByIdAsync(postId)
            ?? throw new ArgumentException("Post not found");
        var site = await _siteService.GetBySlugAsync("default")
            ?? throw new ArgumentException("Site not found");
        var subscribers = await GetActiveSubscribersAsync(siteId);
        var subscriberList = subscribers.ToList();

        var campaign = new NewsletterCampaign
        {
            Id = Guid.NewGuid(),
            SiteId = siteId,
            PostId = postId,
            Subject = post.Title,
            BodyHtml = BuildNewsletterHtml(post, site.Name),
            Status = "sending",
            CreatedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(campaign);

        try
        {
            var emails = subscriberList.Select(s => s.Email).ToList();
            var tokenMap = subscriberList.ToDictionary(s => s.Email, s => s.UnsubscribeToken);

            await _emailService.SendBulkAsync(
                emails,
                campaign.Subject,
                campaign.BodyHtml,
                email =>
                {
                    var token = tokenMap.GetValueOrDefault(email, "");
                    return campaign.BodyHtml.Replace("{{UNSUBSCRIBE_TOKEN}}", token);
                });

            campaign.Status = "sent";
            campaign.SentCount = subscriberList.Count;
            campaign.SentAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Campaign send failed for post {PostId}", postId);
            campaign.Status = "failed";
            campaign.FailedCount = subscriberList.Count;
        }

        await _db.UpdateAsync(campaign);
        return campaign;
    }

    public async Task<NewsletterCampaign?> GetCampaignAsync(Guid campaignId)
    {
        Guard.Against.Default(campaignId);
        return await _db.GetAsync<NewsletterCampaign>(campaignId);
    }

    public async Task<IEnumerable<NewsletterCampaign>> GetCampaignsAsync(Guid siteId, int page = 1, int pageSize = 20)
    {
        Guard.Against.Default(siteId);
        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<NewsletterCampaign>(
            "SELECT * FROM newsletter_campaigns WHERE site_id = @SiteId ORDER BY created_at DESC LIMIT @Limit OFFSET @Offset",
            new { SiteId = siteId, Limit = pageSize, Offset = offset });
    }

    private string GenerateUnsubscribeToken(string email, Guid siteId)
    {
        var secret = _configuration["Contento:UnsubscribeSecret"] ?? "contento-default-secret";
        var input = $"{email}:{siteId}:{secret}";
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string BuildNewsletterHtml(Post post, string siteName)
    {
        var title = System.Net.WebUtility.HtmlEncode(post.Title);
        var subtitle = post.Subtitle != null
            ? $"<p style=\"color: #6B6B6B; font-size: 16px; margin-bottom: 16px;\">{System.Net.WebUtility.HtmlEncode(post.Subtitle)}</p>"
            : "";
        var body = post.BodyHtml ?? post.BodyMarkdown;
        var site = System.Net.WebUtility.HtmlEncode(siteName);

        return $$"""
            <div style="font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif; max-width: 600px; margin: 0 auto; padding: 24px;">
                <h1 style="color: #1A1A1A; font-size: 24px; margin-bottom: 8px;">{{title}}</h1>
                {{subtitle}}
                <div style="color: #1A1A1A; line-height: 1.6;">
                    {{body}}
                </div>
                <hr style="border: none; border-top: 1px solid #E5E5E5; margin: 32px 0 16px;" />
                <p style="color: #999; font-size: 12px;">
                    You received this because you subscribed to {{site}}.
                    <a href="{UNSUBSCRIBE_URL}?token={UNSUBSCRIBE_TOKEN}" style="color: #999;">Unsubscribe</a>
                </p>
            </div>
            """;
    }
}
