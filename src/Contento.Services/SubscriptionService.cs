using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Stripe;
using Stripe.Checkout;

namespace Contento.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IDbConnection _db;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly IMembershipPlanService _membershipPlanService;

    public SubscriptionService(IDbConnection db, IConfiguration configuration, ILogger<SubscriptionService> logger,
        IMembershipPlanService membershipPlanService)
    {
        _db = Guard.Against.Null(db);
        _configuration = Guard.Against.Null(configuration);
        _logger = Guard.Against.Null(logger);
        _membershipPlanService = Guard.Against.Null(membershipPlanService);
    }

    public async Task<string> CreateCheckoutSessionAsync(Guid siteId, string email, string priceId,
        string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            Mode = "subscription",
            CustomerEmail = email,
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["site_id"] = siteId.ToString(),
                ["email"] = email
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation("Stripe checkout session created for {Email}, site {SiteId}", email, siteId);
        return session.Url;
    }

    public async Task<string> CreateBillingPortalSessionAsync(string stripeCustomerId, string returnUrl)
    {
        var options = new Stripe.BillingPortal.SessionCreateOptions
        {
            Customer = stripeCustomerId,
            ReturnUrl = returnUrl
        };

        var service = new Stripe.BillingPortal.SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task ProvisionSubscriptionAsync(string stripeCustomerId, string subscriptionId,
        string email, Guid siteId)
    {
        // Look up the membership plan by matching Stripe price ID from the subscription
        MembershipPlan? plan = null;
        string? priceId = null;
        if (!string.IsNullOrEmpty(subscriptionId))
        {
            try
            {
                var stripeSub = await new Stripe.SubscriptionService().GetAsync(subscriptionId);
                priceId = stripeSub.Items?.Data?.FirstOrDefault()?.Price?.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Could not fetch Stripe subscription {SubscriptionId}", subscriptionId);
            }
        }

        if (!string.IsNullOrEmpty(priceId))
        {
            var plans = await _membershipPlanService.GetAllAsync(siteId);
            plan = plans.FirstOrDefault(p => p.StripePriceId == priceId);
        }

        var tierName = plan?.Name?.ToLowerInvariant() ?? "premium";
        var expiresAt = DateTime.UtcNow.AddMonths(1);

        var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE site_id = @SiteId AND email = @Email",
            new { SiteId = siteId, Email = email.ToLowerInvariant() });

        if (subscriber == null)
        {
            subscriber = new Subscriber
            {
                Id = Guid.NewGuid(),
                SiteId = siteId,
                Email = email.ToLowerInvariant(),
                Status = "active",
                StripeCustomerId = stripeCustomerId,
                StripeSubscriptionId = subscriptionId,
                MembershipTier = tierName,
                MembershipPlanId = plan?.Id,
                MembershipExpiresAt = expiresAt,
                PaymentFailureCount = 0,
                UnsubscribeToken = Convert.ToHexString(
                    System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)).ToLowerInvariant(),
                SubscribedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Apply trial if plan has trial days
            if (plan != null && plan.TrialDays > 0)
            {
                subscriber.TrialEndsAt = DateTime.UtcNow.AddDays(plan.TrialDays);
            }

            await _db.InsertAsync(subscriber);
        }
        else
        {
            subscriber.StripeCustomerId = stripeCustomerId;
            subscriber.StripeSubscriptionId = subscriptionId;
            subscriber.MembershipTier = tierName;
            subscriber.MembershipPlanId = plan?.Id;
            subscriber.MembershipExpiresAt = expiresAt;
            subscriber.PaymentFailureCount = 0;
            subscriber.UpdatedAt = DateTime.UtcNow;

            // Apply trial if plan has trial days and subscriber doesn't already have one
            if (plan != null && plan.TrialDays > 0 && !subscriber.TrialEndsAt.HasValue)
            {
                subscriber.TrialEndsAt = DateTime.UtcNow.AddDays(plan.TrialDays);
            }

            await _db.UpdateAsync(subscriber);
        }

        _logger.LogInformation("Subscription provisioned for {Email}, site {SiteId}, plan {Plan}",
            email, siteId, tierName);
    }

    public async Task UpdateSubscriptionStatusAsync(string subscriptionId, string status)
    {
        var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE stripe_subscription_id = @SubscriptionId",
            new { SubscriptionId = subscriptionId });

        if (subscriber == null)
        {
            _logger.LogWarning("No subscriber found for subscription {SubscriptionId}", subscriptionId);
            return;
        }

        switch (status)
        {
            case "active":
                subscriber.Status = "active";
                subscriber.MembershipExpiresAt = DateTime.UtcNow.AddMonths(1);
                subscriber.PaymentFailureCount = 0;
                break;
            case "past_due":
                subscriber.Status = "active"; // still active but payment is overdue
                break;
            case "canceled":
            case "unpaid":
                subscriber.MembershipTier = "free";
                subscriber.MembershipExpiresAt = null;
                subscriber.MembershipPlanId = null;
                break;
            default:
                _logger.LogInformation("Unhandled subscription status {Status} for {SubscriptionId}", status, subscriptionId);
                break;
        }

        subscriber.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(subscriber);

        _logger.LogInformation("Subscription {SubscriptionId} status updated to {Status} for {Email}",
            subscriptionId, status, subscriber.Email);
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        // Find subscriber by stripe customer ID from the subscription
        var subscriptionService = new Stripe.SubscriptionService();
        var subscription = await subscriptionService.GetAsync(subscriptionId);

        var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE stripe_customer_id = @CustomerId",
            new { CustomerId = subscription.CustomerId });

        if (subscriber != null)
        {
            subscriber.MembershipTier = "free";
            subscriber.MembershipExpiresAt = null;
            subscriber.UpdatedAt = DateTime.UtcNow;
            await _db.UpdateAsync(subscriber);

            _logger.LogInformation("Subscription cancelled for {Email}", subscriber.Email);
        }
    }

    public async Task HandlePaymentFailureAsync(string subscriptionId)
    {
        var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE stripe_subscription_id = @SubscriptionId",
            new { SubscriptionId = subscriptionId });

        if (subscriber == null)
        {
            _logger.LogWarning("Payment failed for subscription {SubscriptionId} but no subscriber found", subscriptionId);
            return;
        }

        subscriber.PaymentFailureCount += 1;
        subscriber.UpdatedAt = DateTime.UtcNow;

        _logger.LogWarning("Payment failed for subscription {SubscriptionId}, email {Email}, failure count {Count}",
            subscriptionId, subscriber.Email, subscriber.PaymentFailureCount);

        // After 3 consecutive failures, downgrade to free tier
        if (subscriber.PaymentFailureCount >= 3)
        {
            subscriber.MembershipTier = "free";
            subscriber.MembershipExpiresAt = null;
            subscriber.MembershipPlanId = null;
            _logger.LogWarning("Subscriber {Email} downgraded to free tier after {Count} consecutive payment failures",
                subscriber.Email, subscriber.PaymentFailureCount);
        }

        await _db.UpdateAsync(subscriber);
    }

    public async Task<bool> HasActiveMembershipAsync(string email, Guid siteId)
    {
        var tier = await GetMembershipTierAsync(email, siteId);
        return tier != null && tier != "free";
    }

    public async Task<string?> GetMembershipTierAsync(string email, Guid siteId)
    {
        var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
            "SELECT * FROM subscribers WHERE site_id = @SiteId AND email = @Email AND status = 'active'",
            new { SiteId = siteId, Email = email.ToLowerInvariant() });

        if (subscriber == null) return null;

        // Check if membership expired (with 3-day grace period)
        if (subscriber.MembershipExpiresAt.HasValue && subscriber.MembershipExpiresAt.Value.AddDays(3) < DateTime.UtcNow)
            return "free";

        return subscriber.MembershipTier;
    }
}
