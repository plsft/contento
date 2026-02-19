namespace Contento.Core.Interfaces;

public interface ISubscriptionService
{
    Task<string> CreateCheckoutSessionAsync(Guid siteId, string email, string priceId, string successUrl, string cancelUrl);
    Task<string> CreateBillingPortalSessionAsync(string stripeCustomerId, string returnUrl);
    Task ProvisionSubscriptionAsync(string stripeCustomerId, string subscriptionId, string email, Guid siteId);
    Task UpdateSubscriptionStatusAsync(string subscriptionId, string status);
    Task CancelSubscriptionAsync(string subscriptionId);
    Task HandlePaymentFailureAsync(string subscriptionId);
    Task<bool> HasActiveMembershipAsync(string email, Guid siteId);
    Task<string?> GetMembershipTierAsync(string email, Guid siteId);
}
