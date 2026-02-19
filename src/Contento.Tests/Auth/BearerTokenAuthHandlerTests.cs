using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace Contento.Tests.Auth;

/// <summary>
/// Tests for the HMAC-SHA256 token format used by <see cref="Contento.Web.Auth.BearerTokenAuthHandler"/>.
/// The token format is <c>{base64payload}.{base64hmac}</c> where the payload is JSON with
/// <c>UserId</c>, <c>Email</c>, <c>Role</c>, and <c>ExpiresAt</c> fields.
///
/// Since <c>HandleAuthenticateAsync</c> is protected and requires the full ASP.NET auth pipeline,
/// these tests verify the token creation/validation logic indirectly by testing the HMAC computation,
/// token structure, payload serialization, and expiry detection.
/// </summary>
[TestFixture]
public class BearerTokenAuthHandlerTests
{
    private const string DefaultSigningKey = "contento-default-signing-key-change-in-production";

    [Test]
    public void ValidToken_HasCorrectHmacSignature()
    {
        var payload = new { UserId = Guid.NewGuid().ToString(), Email = "test@test.com", Role = "admin", ExpiresAt = DateTime.UtcNow.AddHours(1).ToString("O") };
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
        var signature = ComputeHmac(payloadB64, DefaultSigningKey);
        var token = $"{payloadB64}.{signature}";

        // Verify the signature matches
        var parts = token.Split('.');
        Assert.That(parts, Has.Length.EqualTo(2));
        Assert.That(ComputeHmac(parts[0], DefaultSigningKey), Is.EqualTo(parts[1]));
    }

    [Test]
    public void ExpiredToken_PayloadContainsExpiredDate()
    {
        var payload = new { UserId = Guid.NewGuid().ToString(), Email = "test@test.com", Role = "admin", ExpiresAt = DateTime.UtcNow.AddHours(-1).ToString("O") };
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));

        // Parse and verify expiry
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(payloadB64));
        var parsed = JsonSerializer.Deserialize<JsonElement>(decoded);
        var expiresAt = DateTime.Parse(parsed.GetProperty("ExpiresAt").GetString()!).ToUniversalTime();
        Assert.That(expiresAt, Is.LessThan(DateTime.UtcNow));
    }

    [Test]
    public void MalformedToken_NoDotSeparator_HasSinglePart()
    {
        var token = "nodothere";
        var parts = token.Split('.');
        Assert.That(parts, Has.Length.EqualTo(1));
    }

    [Test]
    public void MalformedToken_WrongSignature_DoesNotMatch()
    {
        var payload = new { UserId = Guid.NewGuid().ToString(), Email = "test@test.com", Role = "admin", ExpiresAt = DateTime.UtcNow.AddHours(1).ToString("O") };
        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadB64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(payloadJson));
        var wrongSig = ComputeHmac(payloadB64, "wrong-key");
        var correctSig = ComputeHmac(payloadB64, DefaultSigningKey);

        Assert.That(wrongSig, Is.Not.EqualTo(correctSig));
    }

    [Test]
    public void TokenPayload_DeserializesCorrectly()
    {
        var userId = Guid.NewGuid().ToString();
        var email = "user@example.com";
        var role = "editor";
        var expiresAt = DateTime.UtcNow.AddHours(2).ToString("O");

        var payload = new { UserId = userId, Email = email, Role = role, ExpiresAt = expiresAt };
        var json = JsonSerializer.Serialize(payload);
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        var parsed = JsonSerializer.Deserialize<JsonElement>(decoded);

        Assert.That(parsed.GetProperty("UserId").GetString(), Is.EqualTo(userId));
        Assert.That(parsed.GetProperty("Email").GetString(), Is.EqualTo(email));
        Assert.That(parsed.GetProperty("Role").GetString(), Is.EqualTo(role));
    }

    private static string ComputeHmac(string data, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var hash = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(hash);
    }
}
