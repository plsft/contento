using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

[Tags("Membership")]
[ApiController]
[Route("api/v1/membership")]
public class MembershipApiController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ISiteService _siteService;
    private readonly IConfiguration _config;

    public MembershipApiController(ISubscriptionService subscriptionService, ISiteService siteService, IConfiguration config)
    {
        _subscriptionService = subscriptionService;
        _siteService = siteService;
        _config = config;
    }

    [HttpPost("checkout")]
    [AllowAnonymous]
    [EndpointSummary("Create membership checkout session")]
    [EndpointDescription("Creates a Stripe Checkout session for a new membership subscription. Returns the redirect URL for the Stripe-hosted payment page.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = new { code = "INVALID_EMAIL", message = "Email is required." } });

        var priceId = request.PriceId;
        if (string.IsNullOrEmpty(priceId))
        {
            // Fallback to config for backwards compatibility
            priceId = _config["Stripe:PremiumPriceId"];
        }

        if (string.IsNullOrEmpty(priceId) || priceId.StartsWith("${"))
            return BadRequest(new { error = new { code = "NOT_CONFIGURED", message = "Membership is not configured." } });

        var siteId = HttpContext.GetCurrentSiteId();

        var successUrl = $"{Request.Scheme}://{Request.Host}/membership?success=true";
        var cancelUrl = $"{Request.Scheme}://{Request.Host}/membership?canceled=true";

        var sessionUrl = await _subscriptionService.CreateCheckoutSessionAsync(siteId, request.Email, priceId, successUrl, cancelUrl);
        return Ok(new { data = new { url = sessionUrl } });
    }

    [HttpPost("portal")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("Create billing portal session")]
    [EndpointDescription("Creates a Stripe Billing Portal session for an existing subscriber to manage their subscription, payment method, and invoices.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreatePortalSession([FromBody] PortalRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StripeCustomerId))
            return BadRequest(new { error = new { code = "INVALID_CUSTOMER", message = "Stripe customer ID is required." } });

        var returnUrl = $"{Request.Scheme}://{Request.Host}/membership";
        var url = await _subscriptionService.CreateBillingPortalSessionAsync(request.StripeCustomerId, returnUrl);
        return Ok(new { data = new { url } });
    }

    [HttpGet("status")]
    [AllowAnonymous]
    [EndpointSummary("Check membership status")]
    [EndpointDescription("Checks whether an email address has an active membership subscription and returns the current membership tier.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CheckStatus([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { error = new { code = "INVALID_EMAIL", message = "Email is required." } });

        var siteId = HttpContext.GetCurrentSiteId();

        var hasActive = await _subscriptionService.HasActiveMembershipAsync(email, siteId);
        var tier = await _subscriptionService.GetMembershipTierAsync(email, siteId);

        return Ok(new { data = new { active = hasActive, tier = tier ?? "free" } });
    }

    public record CheckoutRequest(string Email, string? PriceId = null);
    public record PortalRequest(string StripeCustomerId);
}
