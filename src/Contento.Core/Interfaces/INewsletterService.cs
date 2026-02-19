using Contento.Core.Models;

namespace Contento.Core.Interfaces;

public interface INewsletterService
{
    Task<Subscriber?> SubscribeAsync(Guid siteId, string email, string? displayName = null);
    Task<bool> UnsubscribeAsync(string token);
    Task<IEnumerable<Subscriber>> GetActiveSubscribersAsync(Guid siteId);
    Task<int> GetSubscriberCountAsync(Guid siteId);
    Task<NewsletterCampaign> SendCampaignAsync(Guid siteId, Guid postId);
    Task<NewsletterCampaign?> GetCampaignAsync(Guid campaignId);
    Task<IEnumerable<NewsletterCampaign>> GetCampaignsAsync(Guid siteId, int page = 1, int pageSize = 20);
}
