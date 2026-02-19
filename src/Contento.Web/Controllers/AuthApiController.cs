using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;

namespace Contento.Web.Controllers;

/// <summary>
/// API authentication — issue and validate bearer tokens for headless access.
/// Tokens are HMAC-SHA256 signed, stateless, and expire after a configurable TTL.
/// </summary>
[Tags("Authentication")]
[ApiController]
[Route("api/v1/auth")]
[AllowAnonymous]
public class AuthApiController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _config;
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;

    private static readonly TimeSpan DefaultTokenLifetime = TimeSpan.FromHours(24);

    public AuthApiController(
        IUserService userService,
        IConfiguration config,
        IEmailService emailService,
        INotificationService notificationService)
    {
        _userService = userService;
        _config = config;
        _emailService = emailService;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Exchange email + password for a bearer token.
    /// POST /api/v1/auth/token
    /// </summary>
    [HttpPost("token")]
    [EndpointSummary("Create authentication token")]
    [EndpointDescription("Exchanges email and password credentials for an HMAC-SHA256 signed bearer token with a 24-hour expiry.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateToken([FromBody] TokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Email and password are required." } });

        var user = await _userService.ValidatePasswordAsync(request.Email, request.Password);
        if (user == null)
            return Unauthorized(new { error = new { code = "INVALID_CREDENTIALS", message = "Invalid email or password." } });

        if (!user.IsActive)
            return Unauthorized(new { error = new { code = "ACCOUNT_DISABLED", message = "This account has been disabled." } });

        await _userService.UpdateLastLoginAsync(user.Id);

        var expiresAt = DateTime.UtcNow.Add(DefaultTokenLifetime);
        var token = GenerateToken(user.Id, user.Email, user.Role, expiresAt);

        return Ok(new
        {
            data = new
            {
                token,
                tokenType = "Bearer",
                expiresAt,
                expiresIn = (int)DefaultTokenLifetime.TotalSeconds,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    displayName = user.DisplayName,
                    role = user.Role
                }
            }
        });
    }

    /// <summary>
    /// Refresh an existing valid token.
    /// POST /api/v1/auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [EndpointSummary("Refresh authentication token")]
    [EndpointDescription("Exchanges a valid, non-expired bearer token for a new token with a fresh 24-hour expiry window.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Token is required." } });

        var payload = ValidateToken(request.Token);
        if (payload == null)
            return Unauthorized(new { error = new { code = "INVALID_TOKEN", message = "Token is invalid or expired." } });

        var userId = Guid.Parse(payload.UserId);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
            return Unauthorized(new { error = new { code = "INVALID_TOKEN", message = "User no longer exists or is disabled." } });

        var expiresAt = DateTime.UtcNow.Add(DefaultTokenLifetime);
        var newToken = GenerateToken(user.Id, user.Email, user.Role, expiresAt);

        return Ok(new
        {
            data = new
            {
                token = newToken,
                tokenType = "Bearer",
                expiresAt,
                expiresIn = (int)DefaultTokenLifetime.TotalSeconds
            }
        });
    }

    /// <summary>
    /// Validate a token and return the associated user.
    /// GET /api/v1/auth/me
    /// </summary>
    [HttpGet("me")]
    [EndpointSummary("Get current user")]
    [EndpointDescription("Validates the bearer token and returns the authenticated user's profile including email, display name, and role.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var token = ExtractBearerToken();
        if (token == null)
            return Unauthorized(new { error = new { code = "NO_TOKEN", message = "Bearer token required." } });

        var payload = ValidateToken(token);
        if (payload == null)
            return Unauthorized(new { error = new { code = "INVALID_TOKEN", message = "Token is invalid or expired." } });

        var userId = Guid.Parse(payload.UserId);
        var user = await _userService.GetByIdAsync(userId);
        if (user == null)
            return NotFound(new { error = new { code = "NOT_FOUND", message = "User not found." } });

        return Ok(new
        {
            data = new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                role = user.Role,
                isActive = user.IsActive
            }
        });
    }

    /// <summary>
    /// Request a password reset email.
    /// POST /api/v1/auth/forgot-password
    /// </summary>
    [HttpPost("forgot-password")]
    [EndpointSummary("Request password reset")]
    [EndpointDescription("Sends a password reset email if the account exists. Always returns 200 to prevent email enumeration.")]
    [ProducesResponseType(200)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Email is required." } });

        var token = await _userService.RequestPasswordResetAsync(request.Email);

        // Only send email if the user actually exists (token was stored)
        var user = await _userService.GetByEmailAsync(request.Email);
        if (user != null)
        {
            var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={token}";
            await _emailService.SendAsync(
                user.Email,
                "Reset your password",
                $"<p>Click <a href=\"{resetUrl}\">here</a> to reset your password. This link expires in 1 hour.</p>");
        }

        // Always return success to prevent email enumeration
        return Ok(new { data = new { message = "If an account with that email exists, a password reset link has been sent." } });
    }

    /// <summary>
    /// Reset password using a valid token.
    /// POST /api/v1/auth/reset-password
    /// </summary>
    [HttpPost("reset-password")]
    [EndpointSummary("Reset password")]
    [EndpointDescription("Resets the user's password using a valid, non-expired password reset token.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Token and new password are required." } });

        var success = await _userService.ResetPasswordAsync(request.Token, request.NewPassword);
        if (!success)
            return BadRequest(new { error = new { code = "INVALID_TOKEN", message = "Reset token is invalid or expired." } });

        return Ok(new { data = new { message = "Password has been reset successfully." } });
    }

    /// <summary>
    /// Register a new public user account.
    /// POST /api/v1/auth/register
    /// </summary>
    [HttpPost("register")]
    [EndpointSummary("Register new user")]
    [EndpointDescription("Creates a new user account with the 'viewer' role and sends a verification email.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.DisplayName))
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Email, password, and display name are required." } });

        // Check if user already exists
        var existing = await _userService.GetByEmailAsync(request.Email);
        if (existing != null)
            return BadRequest(new { error = new { code = "EMAIL_EXISTS", message = "An account with this email already exists." } });

        var user = await _userService.RegisterAsync(request.Email, request.Password, request.DisplayName);

        // Send verification email
        var verifyUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/verify-email?token={user.EmailVerificationToken}";
        await _emailService.SendAsync(
            user.Email,
            "Verify your email",
            $"<p>Welcome to Contento! Click <a href=\"{verifyUrl}\">here</a> to verify your email address.</p>");

        return Ok(new
        {
            data = new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                message = "Registration successful. Please check your email to verify your account."
            }
        });
    }

    /// <summary>
    /// Verify email address using a verification token.
    /// POST /api/v1/auth/verify-email
    /// </summary>
    [HttpPost("verify-email")]
    [EndpointSummary("Verify email address")]
    [EndpointDescription("Verifies a user's email address using the token sent during registration.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { error = new { code = "INVALID_REQUEST", message = "Verification token is required." } });

        var success = await _userService.VerifyEmailAsync(request.Token);
        if (!success)
            return BadRequest(new { error = new { code = "INVALID_TOKEN", message = "Verification token is invalid." } });

        return Ok(new { data = new { message = "Email verified successfully." } });
    }

    // --- Token generation/validation ---

    private string GetSigningKey()
    {
        var key = _config["Contento:ApiSigningKey"]
            ?? Environment.GetEnvironmentVariable("API_SIGNING_KEY")
            ?? "contento-default-signing-key-change-in-production";
        return key;
    }

    private string GenerateToken(Guid userId, string email, string role, DateTime expiresAt)
    {
        var payload = JsonSerializer.Serialize(new TokenPayload
        {
            UserId = userId.ToString(),
            Email = email,
            Role = role,
            ExpiresAt = expiresAt.ToString("O")
        });

        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var payloadB64 = Convert.ToBase64String(payloadBytes);

        var signature = ComputeHmac(payloadB64);
        return $"{payloadB64}.{signature}";
    }

    private TokenPayload? ValidateToken(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 2) return null;

        var payloadB64 = parts[0];
        var signature = parts[1];

        var expectedSig = ComputeHmac(payloadB64);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expectedSig)))
            return null;

        try
        {
            var payloadBytes = Convert.FromBase64String(payloadB64);
            var payload = JsonSerializer.Deserialize<TokenPayload>(Encoding.UTF8.GetString(payloadBytes));
            if (payload == null) return null;

            var expiresAt = DateTime.Parse(payload.ExpiresAt).ToUniversalTime();
            if (expiresAt < DateTime.UtcNow) return null;

            return payload;
        }
        catch
        {
            return null;
        }
    }

    private string ComputeHmac(string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(GetSigningKey());
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hash);
    }

    private string? ExtractBearerToken()
    {
        var header = Request.Headers.Authorization.FirstOrDefault();
        if (header != null && header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return header["Bearer ".Length..].Trim();
        return null;
    }
}

public class TokenRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshRequest
{
    public string Token { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public class VerifyEmailRequest
{
    public string Token { get; set; } = string.Empty;
}

public class TokenPayload
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
}
