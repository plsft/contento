using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Stripe;

namespace Contento.Web.Controllers;

[Tags("Stripe Webhooks")]
[ApiController]
[Route("api/v1/stripe")]
[AllowAnonymous]
public class StripeWebhookController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly IMembershipPlanService _membershipPlanService;
    private readonly ISiteService _siteService;
    private readonly IConfiguration _config;
    private readonly IDbConnection _db;
    private readonly ILogger<StripeWebhookController> _logger;

    public StripeWebhookController(
        ISubscriptionService subscriptionService,
        IMembershipPlanService membershipPlanService,
        ISiteService siteService,
        IConfiguration config,
        IDbConnection db,
        ILogger<StripeWebhookController> logger)
    {
        _subscriptionService = subscriptionService;
        _membershipPlanService = membershipPlanService;
        _siteService = siteService;
        _config = config;
        _db = db;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [EndpointSummary("Handle Stripe webhook")]
    [EndpointDescription("Receives and processes Stripe webhook events including checkout completions, subscription updates, cancellations, and payment failures. Verified via webhook signature.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> HandleWebhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        var webhookSecret = _config["Stripe:WebhookSecret"];

        if (string.IsNullOrEmpty(webhookSecret) || webhookSecret.StartsWith("${"))
        {
            _logger.LogWarning("Stripe webhook secret not configured");
            return Ok();
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Invalid Stripe webhook signature");
            return BadRequest();
        }

        // Idempotency check
        var existing = await _db.QueryFirstOrDefaultAsync<StripeEvent>(
            "SELECT * FROM stripe_events WHERE event_id = @EventId",
            new { EventId = stripeEvent.Id });
        if (existing != null)
            return Ok();

        // Log the event
        await _db.InsertAsync(new StripeEvent
        {
            EventId = stripeEvent.Id,
            EventType = stripeEvent.Type,
            ProcessedAt = DateTime.UtcNow
        });

        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                {
                    var session = stripeEvent.Data.Object as Stripe.Checkout.Session;
                    if (session != null)
                    {
                        var email = session.CustomerEmail ?? session.Metadata?.GetValueOrDefault("email");
                        var customerId = session.CustomerId;
                        var subscriptionId = session.SubscriptionId;
                        if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(customerId))
                        {
                            await _subscriptionService.ProvisionSubscriptionAsync(customerId, subscriptionId ?? "", email, siteId);
                        }
                    }
                    break;
                }
                case "customer.subscription.updated":
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    if (subscription != null)
                    {
                        // Update subscription status (handles active, past_due, canceled, unpaid)
                        await _subscriptionService.UpdateSubscriptionStatusAsync(subscription.Id, subscription.Status);

                        // If the plan/price changed, update the subscriber's membership plan
                        var newPriceId = subscription.Items?.Data?.FirstOrDefault()?.Price?.Id;
                        if (!string.IsNullOrEmpty(newPriceId))
                        {
                            var subscriber = await _db.QueryFirstOrDefaultAsync<Subscriber>(
                                "SELECT * FROM subscribers WHERE stripe_customer_id = @CustomerId",
                                new { CustomerId = subscription.CustomerId });

                            if (subscriber != null)
                            {
                                var plans = await _membershipPlanService.GetAllAsync(subscriber.SiteId);
                                var matchedPlan = plans.FirstOrDefault(p => p.StripePriceId == newPriceId);
                                if (matchedPlan != null)
                                {
                                    subscriber.MembershipPlanId = matchedPlan.Id;
                                    subscriber.MembershipTier = matchedPlan.Name.ToLowerInvariant();
                                    subscriber.MembershipExpiresAt = DateTime.UtcNow.AddMonths(1);
                                    subscriber.UpdatedAt = DateTime.UtcNow;
                                    await _db.UpdateAsync(subscriber);
                                    _logger.LogInformation("Subscriber {Email} plan updated to {Plan}",
                                        subscriber.Email, matchedPlan.Name);
                                }
                            }
                        }
                    }
                    break;
                }
                case "customer.subscription.deleted":
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    if (subscription != null)
                    {
                        await _subscriptionService.CancelSubscriptionAsync(subscription.Id);
                    }
                    break;
                }
                case "invoice.payment_failed":
                {
                    var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                    var failedSubId = GetSubscriptionIdFromInvoice(invoice);
                    if (failedSubId != null)
                    {
                        await _subscriptionService.HandlePaymentFailureAsync(failedSubId);
                    }
                    break;
                }
                case "invoice.paid":
                {
                    var invoice = stripeEvent.Data.Object as Stripe.Invoice;
                    var paidSubId = GetSubscriptionIdFromInvoice(invoice);
                    if (paidSubId != null)
                    {
                        await _subscriptionService.UpdateSubscriptionStatusAsync(paidSubId, "active");
                    }
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook event {EventType}", stripeEvent.Type);
        }

        return Ok();
    }

    /// <summary>
    /// Extracts the subscription ID from an Invoice object.
    /// In Stripe API 2025-03-31 (Stripe.net v50), the subscription is accessed
    /// via Invoice.Parent.SubscriptionDetails.Subscription instead of Invoice.SubscriptionId.
    /// </summary>
    private static string? GetSubscriptionIdFromInvoice(Stripe.Invoice? invoice)
    {
        if (invoice == null) return null;
        return invoice.Parent?.SubscriptionDetails?.Subscription?.Id;
    }
}
