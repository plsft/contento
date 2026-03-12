using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="GravatarHelper"/>.
/// Validates URL generation, MD5 hashing, email normalization,
/// and default/fallback behaviour.
/// </summary>
[TestFixture]
public class GravatarHelperTests
{
    // ---------------------------------------------------------------
    // Null / empty / whitespace email returns default URL
    // ---------------------------------------------------------------

    [Test]
    public void GetAvatarUrl_NullEmail_ReturnsDefaultUrl()
    {
        var url = GravatarHelper.GetAvatarUrl(null);

        Assert.That(url, Is.EqualTo("https://www.gravatar.com/avatar/?d=mp&s=80"));
    }

    [Test]
    public void GetAvatarUrl_EmptyEmail_ReturnsDefaultUrl()
    {
        var url = GravatarHelper.GetAvatarUrl("");

        Assert.That(url, Is.EqualTo("https://www.gravatar.com/avatar/?d=mp&s=80"));
    }

    [Test]
    public void GetAvatarUrl_WhitespaceEmail_ReturnsDefaultUrl()
    {
        var url = GravatarHelper.GetAvatarUrl("   ");

        Assert.That(url, Is.EqualTo("https://www.gravatar.com/avatar/?d=mp&s=80"));
    }

    // ---------------------------------------------------------------
    // Valid email generates correct MD5 hash
    // ---------------------------------------------------------------

    [Test]
    public void GetAvatarUrl_ValidEmail_GeneratesCorrectMd5Hash()
    {
        // "test@example.com" trimmed + lowered is "test@example.com"
        var expected = Convert.ToHexStringLower(
            MD5.HashData(Encoding.UTF8.GetBytes("test@example.com")));

        var url = GravatarHelper.GetAvatarUrl("test@example.com");

        Assert.That(url, Does.Contain($"/avatar/{expected}?"));
    }

    [Test]
    public void GetAvatarUrl_ValidEmail_ReturnsGravatarDomain()
    {
        var url = GravatarHelper.GetAvatarUrl("user@domain.com");

        Assert.That(url, Does.StartWith("https://www.gravatar.com/avatar/"));
    }

    // ---------------------------------------------------------------
    // Size parameter is included in URL
    // ---------------------------------------------------------------

    [Test]
    public void GetAvatarUrl_DefaultSize_Includes80InUrl()
    {
        var url = GravatarHelper.GetAvatarUrl("test@example.com");

        Assert.That(url, Does.Contain("s=80"));
    }

    [Test]
    public void GetAvatarUrl_CustomSize_IncludesSizeInUrl()
    {
        var url = GravatarHelper.GetAvatarUrl("test@example.com", size: 200);

        Assert.That(url, Does.Contain("s=200"));
    }

    [Test]
    public void GetAvatarUrl_NullEmailCustomSize_IncludesSizeInUrl()
    {
        var url = GravatarHelper.GetAvatarUrl(null, size: 120);

        Assert.That(url, Does.Contain("s=120"));
    }

    // ---------------------------------------------------------------
    // Email is lowercased and trimmed before hashing
    // ---------------------------------------------------------------

    [Test]
    public void GetAvatarUrl_UppercaseEmail_ProducesSameHashAsLowercase()
    {
        var urlLower = GravatarHelper.GetAvatarUrl("test@example.com");
        var urlUpper = GravatarHelper.GetAvatarUrl("TEST@EXAMPLE.COM");

        Assert.That(urlUpper, Is.EqualTo(urlLower));
    }

    [Test]
    public void GetAvatarUrl_MixedCaseEmail_ProducesSameHashAsLowercase()
    {
        var urlLower = GravatarHelper.GetAvatarUrl("test@example.com");
        var urlMixed = GravatarHelper.GetAvatarUrl("Test@Example.COM");

        Assert.That(urlMixed, Is.EqualTo(urlLower));
    }

    [Test]
    public void GetAvatarUrl_EmailWithLeadingTrailingSpaces_ProducesSameHashAsTrimmed()
    {
        var urlTrimmed = GravatarHelper.GetAvatarUrl("test@example.com");
        var urlSpaced = GravatarHelper.GetAvatarUrl("  test@example.com  ");

        Assert.That(urlSpaced, Is.EqualTo(urlTrimmed));
    }

    // ---------------------------------------------------------------
    // Custom default image parameter
    // ---------------------------------------------------------------

    [Test]
    public void GetAvatarUrl_CustomDefaultImage_IncludedInUrl()
    {
        var url = GravatarHelper.GetAvatarUrl("test@example.com", defaultImage: "identicon");

        Assert.That(url, Does.Contain("d=identicon"));
    }

    [Test]
    public void GetAvatarUrl_NullEmailCustomDefaultImage_IncludedInUrl()
    {
        var url = GravatarHelper.GetAvatarUrl(null, defaultImage: "retro");

        Assert.That(url, Does.Contain("d=retro"));
    }

    // ---------------------------------------------------------------
    // Known Gravatar hash (from official docs)
    // ---------------------------------------------------------------

    [Test]
    public void GetAvatarUrl_KnownGravatarHash_MatchesExpected()
    {
        // Well-known: MD5 of "myemailaddress@example.com" = 0bc83cb571cd1c50ba6f3e8a78ef1346
        var expected = Convert.ToHexStringLower(
            MD5.HashData(Encoding.UTF8.GetBytes("myemailaddress@example.com")));

        var url = GravatarHelper.GetAvatarUrl("MyEmailAddress@example.com", 40);

        Assert.That(url, Is.EqualTo($"https://www.gravatar.com/avatar/{expected}?d=mp&s=40"));
    }
}
