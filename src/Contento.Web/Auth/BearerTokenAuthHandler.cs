using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Contento.Web.Controllers;

namespace Contento.Web.Auth;

public class BearerTokenAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly IConfiguration _config;

    public BearerTokenAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config)
        : base(options, logger, encoder)
    {
        _config = config;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var header = Request.Headers.Authorization.FirstOrDefault();
        if (header == null || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(AuthenticateResult.NoResult());

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return Task.FromResult(AuthenticateResult.NoResult());

        var payload = ValidateToken(token);
        if (payload == null)
            return Task.FromResult(AuthenticateResult.Fail("Invalid or expired token."));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, payload.UserId),
            new(ClaimTypes.Email, payload.Email),
            new(ClaimTypes.Role, payload.Role),
            new("app_user_id", payload.UserId)
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
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
        var key = _config["Contento:ApiSigningKey"]
            ?? Environment.GetEnvironmentVariable("API_SIGNING_KEY")
            ?? "contento-default-signing-key-change-in-production";

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hash);
    }
}
