using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;

namespace Contento.Web.Controllers;

[Tags("Newsletter")]
[ApiController]
[Route("api/v1/newsletter")]
public class NewsletterApiController : ControllerBase
{
    private readonly INewsletterService _newsletterService;
    private readonly ISiteService _siteService;

    public NewsletterApiController(INewsletterService newsletterService, ISiteService siteService)
    {
        _newsletterService = newsletterService;
        _siteService = siteService;
    }

    // POST /api/v1/newsletter/subscribe - Anonymous
    [HttpPost("subscribe")]
    [AllowAnonymous]
    [EndpointSummary("Subscribe to newsletter")]
    [EndpointDescription("Subscribes an email address to the site's newsletter. Creates a new subscriber record or reactivates an existing one.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = new { code = "INVALID_EMAIL", message = "Email is required." } });

        var siteId = HttpContext.GetCurrentSiteId();

        var subscriber = await _newsletterService.SubscribeAsync(siteId, request.Email, request.Name);
        return Ok(new { data = new { email = subscriber?.Email, status = subscriber?.Status } });
    }

    // GET /api/v1/newsletter/unsubscribe?token=xxx - Anonymous
    [HttpGet("unsubscribe")]
    [AllowAnonymous]
    [EndpointSummary("Unsubscribe from newsletter")]
    [EndpointDescription("Unsubscribes a user from the newsletter using a unique token sent in the email footer.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Unsubscribe([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { error = new { code = "INVALID_TOKEN", message = "Token is required." } });

        var result = await _newsletterService.UnsubscribeAsync(token);
        return result
            ? Ok(new { data = new { message = "Successfully unsubscribed." } })
            : NotFound(new { error = new { code = "NOT_FOUND", message = "Invalid or expired token." } });
    }

    // GET /api/v1/newsletter/subscribers - Admin only
    [HttpGet("subscribers")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("List newsletter subscribers")]
    [EndpointDescription("Returns all active newsletter subscribers for the current site with a total count. Requires admin authentication.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListSubscribers()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        var subscribers = await _newsletterService.GetActiveSubscribersAsync(siteId);
        var count = await _newsletterService.GetSubscriberCountAsync(siteId);
        return Ok(new { data = subscribers, meta = new { totalCount = count } });
    }

    // POST /api/v1/newsletter/send - Admin only
    [HttpPost("send")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("Send a newsletter campaign")]
    [EndpointDescription("Sends a newsletter campaign based on a published post to all active subscribers. Creates a campaign record for tracking.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> SendCampaign([FromBody] SendCampaignRequest request)
    {
        if (request.PostId == Guid.Empty)
            return BadRequest(new { error = new { code = "INVALID_POST", message = "Post ID is required." } });

        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            var campaign = await _newsletterService.SendCampaignAsync(siteId, request.PostId);
            return Ok(new { data = campaign });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = new { code = "SEND_FAILED", message = ex.Message } });
        }
    }

    // GET /api/v1/newsletter/campaigns - Admin only
    [HttpGet("campaigns")]
    [Authorize(AuthenticationSchemes = "Bearer,Cookies")]
    [EndpointSummary("List newsletter campaigns")]
    [EndpointDescription("Returns a paginated list of newsletter campaigns for the current site with delivery status and statistics.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListCampaigns([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var siteId = HttpContext.GetCurrentSiteId();

        var campaigns = await _newsletterService.GetCampaignsAsync(siteId, page, pageSize);
        return Ok(new { data = campaigns });
    }

    public record SubscribeRequest(string Email, string? Name = null);
    public record SendCampaignRequest(Guid PostId);
}
