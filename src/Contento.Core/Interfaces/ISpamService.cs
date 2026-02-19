using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Anti-spam service for scoring and moderating comments.
/// </summary>
public interface ISpamService
{
    Task<SpamCheckResult> CheckCommentAsync(Comment comment);
    Task TrainHamAsync(Guid commentId);
    Task TrainSpamAsync(Guid commentId);
    Task<SpamStats> GetStatsAsync(Guid siteId);
}

public class SpamCheckResult
{
    public bool IsSpam { get; set; }
    public decimal Score { get; set; }
    public List<string> Reasons { get; set; } = [];
}

public class SpamStats
{
    public int TotalChecked { get; set; }
    public int TotalBlocked { get; set; }
    public int TotalApproved { get; set; }
}
