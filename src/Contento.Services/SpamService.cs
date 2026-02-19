using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Multi-layer spam scoring engine for comment moderation.
/// </summary>
public class SpamService : ISpamService
{
    private readonly IDbConnection _db;
    private readonly ILogger<SpamService> _logger;

    private static readonly Regex SpamPatternRegex = new(
        @"casino|viagra|cialis|crypto.*invest|buy.*cheap|click here now|act now|limited time|make money fast|work from home.*earn|SEO.*rank|backlink.*service",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> DisposableEmailDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "mailinator.com",
        "tempmail.com",
        "guerrillamail.com",
        "throwaway.email",
        "fakeinbox.com",
        "yopmail.com",
        "trashmail.com",
        "10minutemail.com"
    };

    /// <summary>
    /// Initializes a new instance of <see cref="SpamService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public SpamService(IDbConnection db, ILogger<SpamService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<SpamCheckResult> CheckCommentAsync(Comment comment)
    {
        Guard.Against.Null(comment);

        var reasons = new List<string>();
        var totalScore = 0m;

        // 1. Honeypot check (weight 0.05)
        var honeypotScore = 0m;
        if (!string.IsNullOrWhiteSpace(comment.AuthorUrl))
        {
            honeypotScore = 1.0m;
            reasons.Add("Honeypot field filled");
        }
        totalScore += honeypotScore * 0.05m;

        // 2. Link density (weight 0.25)
        var linkCount = 0;
        if (!string.IsNullOrEmpty(comment.BodyMarkdown))
        {
            linkCount += Regex.Matches(comment.BodyMarkdown, @"https?://").Count;
        }
        var linkScore = linkCount > 2 ? 1.0m : linkCount >= 1 ? 0.5m : 0.0m;
        if (linkScore > 0)
            reasons.Add($"Link density: {linkCount} links detected");
        totalScore += linkScore * 0.25m;

        // 3. Known spam patterns (weight 0.20)
        var patternScore = 0m;
        if (!string.IsNullOrEmpty(comment.BodyMarkdown) && SpamPatternRegex.IsMatch(comment.BodyMarkdown))
        {
            patternScore = 1.0m;
            reasons.Add("Known spam pattern detected");
        }
        totalScore += patternScore * 0.20m;

        // 4. Rate limiting (weight 0.15)
        var rateScore = 0m;
        if (!string.IsNullOrWhiteSpace(comment.IpAddress))
        {
            var since = DateTime.UtcNow.AddMinutes(-10);
            var recentCount = await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM comments WHERE ip_address = @Ip AND created_at > @Since",
                new { Ip = comment.IpAddress, Since = since });

            rateScore = recentCount > 3 ? 1.0m : recentCount >= 2 ? 0.5m : 0.0m;
            if (rateScore > 0)
                reasons.Add($"Rate limit: {recentCount} comments in 10 minutes");
        }
        totalScore += rateScore * 0.15m;

        // 5. Body quality (weight 0.10)
        var qualityScore = 0m;
        if (!string.IsNullOrEmpty(comment.BodyMarkdown))
        {
            if (comment.BodyMarkdown.Length > 10 && comment.BodyMarkdown == comment.BodyMarkdown.ToUpperInvariant())
            {
                qualityScore = 0.8m;
                reasons.Add("Body is all caps");
            }
            else if (comment.BodyMarkdown.Length < 10)
            {
                qualityScore = 0.6m;
                reasons.Add("Body too short");
            }
        }
        totalScore += qualityScore * 0.10m;

        // 6. Email pattern (weight 0.10)
        var emailScore = 0m;
        if (!string.IsNullOrWhiteSpace(comment.AuthorEmail))
        {
            var atIndex = comment.AuthorEmail.LastIndexOf('@');
            if (atIndex >= 0)
            {
                var domain = comment.AuthorEmail[(atIndex + 1)..];
                if (DisposableEmailDomains.Contains(domain))
                {
                    emailScore = 1.0m;
                    reasons.Add($"Disposable email domain: {domain}");
                }
            }
        }
        totalScore += emailScore * 0.10m;

        // 7. IP reputation (weight 0.15)
        var ipScore = 0m;
        if (!string.IsNullOrWhiteSpace(comment.IpAddress))
        {
            var results = await _db.QueryAsync<decimal?>(
                @"SELECT AVG(CASE WHEN ss.is_spam THEN 1.0 ELSE 0.0 END)
                  FROM spam_scores ss
                  JOIN comments c ON c.id = ss.comment_id
                  WHERE c.ip_address = @Ip",
                new { Ip = comment.IpAddress });

            var avgScore = results.FirstOrDefault();
            if (avgScore.HasValue)
            {
                ipScore = avgScore.Value;
                if (ipScore > 0)
                    reasons.Add($"IP reputation score: {ipScore:F2}");
            }
        }
        totalScore += ipScore * 0.15m;

        // Determine spam verdict
        var isSpam = totalScore >= 0.6m;

        // Store SpamScore record
        var spamScore = new SpamScore
        {
            Id = Guid.NewGuid(),
            CommentId = comment.Id,
            Score = totalScore,
            Reasons = JsonSerializer.Serialize(reasons),
            IsSpam = isSpam,
            CheckedAt = DateTime.UtcNow
        };
        await _db.InsertAsync(spamScore);

        _logger.LogInformation("Spam check for comment {CommentId}: score={Score}, isSpam={IsSpam}",
            comment.Id, totalScore, isSpam);

        return new SpamCheckResult
        {
            IsSpam = isSpam,
            Score = totalScore,
            Reasons = reasons
        };
    }

    /// <inheritdoc />
    public async Task TrainHamAsync(Guid commentId)
    {
        Guard.Against.Default(commentId);

        var results = await _db.QueryAsync<SpamScore>(
            "SELECT * FROM spam_scores WHERE comment_id = @CommentId LIMIT 1",
            new { CommentId = commentId });

        var spamScore = results.FirstOrDefault();
        if (spamScore != null)
        {
            spamScore.IsSpam = false;
            await _db.UpdateAsync(spamScore);
            _logger.LogInformation("Trained comment {CommentId} as ham", commentId);
        }
    }

    /// <inheritdoc />
    public async Task TrainSpamAsync(Guid commentId)
    {
        Guard.Against.Default(commentId);

        var results = await _db.QueryAsync<SpamScore>(
            "SELECT * FROM spam_scores WHERE comment_id = @CommentId LIMIT 1",
            new { CommentId = commentId });

        var spamScore = results.FirstOrDefault();
        if (spamScore != null)
        {
            spamScore.IsSpam = true;
            await _db.UpdateAsync(spamScore);
            _logger.LogInformation("Trained comment {CommentId} as spam", commentId);
        }
    }

    /// <inheritdoc />
    public async Task<SpamStats> GetStatsAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        var results = await _db.QueryAsync<dynamic>(
            @"SELECT COUNT(*) as total,
                     SUM(CASE WHEN is_spam THEN 1 ELSE 0 END) as blocked
              FROM spam_scores ss
              JOIN comments c ON c.id = ss.comment_id
              WHERE c.post_id IN (SELECT id FROM posts WHERE site_id = @SiteId)",
            new { SiteId = siteId });

        var row = results.FirstOrDefault();
        var total = (int)(row?.total ?? 0);
        var blocked = (int)(row?.blocked ?? 0);

        return new SpamStats
        {
            TotalChecked = total,
            TotalBlocked = blocked,
            TotalApproved = total - blocked
        };
    }
}
