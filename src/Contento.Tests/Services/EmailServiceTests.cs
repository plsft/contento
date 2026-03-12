using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="EmailService"/>. The service depends on <see cref="IConfiguration"/>
/// and <see cref="ILogger{T}"/>. Note that the constructor does NOT use Guard.Against.Null.
/// These tests focus on:
///   - Verifying the service can be instantiated with valid dependencies
///   - Behavior when SMTP is not configured (graceful skip)
///   - Behavior when SMTP host starts with "${" placeholder (graceful skip)
///
/// Tests that exercise actual SMTP sending would require an integration test setup.
/// </summary>
[TestFixture]
public class EmailServiceTests
{
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockConfiguration = new Mock<IConfiguration>();
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor — valid dependencies
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new EmailService(
                _mockConfiguration.Object,
                Mock.Of<ILogger<EmailService>>()));
    }

    // ---------------------------------------------------------------
    // SendAsync — SMTP not configured (host is null)
    // ---------------------------------------------------------------

    [Test]
    public void SendAsync_SmtpHostNull_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        Assert.DoesNotThrowAsync(
            async () => await service.SendAsync("test@test.com", "Test", "<p>Hello</p>"));
    }

    [Test]
    public void SendAsync_SmtpHostEmpty_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("");
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        Assert.DoesNotThrowAsync(
            async () => await service.SendAsync("test@test.com", "Test", "<p>Hello</p>"));
    }

    [Test]
    public void SendAsync_SmtpHostWhitespace_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("   ");
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        Assert.DoesNotThrowAsync(
            async () => await service.SendAsync("test@test.com", "Test Subject", "<p>Body</p>"));
    }

    // ---------------------------------------------------------------
    // SendAsync — SMTP host starts with "${" (env variable placeholder)
    // ---------------------------------------------------------------

    [Test]
    public void SendAsync_SmtpHostPlaceholder_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("${SMTP_HOST}");
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        Assert.DoesNotThrowAsync(
            async () => await service.SendAsync("test@test.com", "Test", "<p>Hello</p>"));
    }

    // ---------------------------------------------------------------
    // SendBulkAsync — SMTP not configured
    // ---------------------------------------------------------------

    [Test]
    public void SendBulkAsync_SmtpHostNull_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        var recipients = new List<string> { "a@test.com", "b@test.com" };

        Assert.DoesNotThrowAsync(
            async () => await service.SendBulkAsync(recipients, "Bulk Test", "<p>Newsletter</p>"));
    }

    [Test]
    public void SendBulkAsync_SmtpHostEmpty_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("");
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        var recipients = new List<string> { "user@example.com" };

        Assert.DoesNotThrowAsync(
            async () => await service.SendBulkAsync(recipients, "Subject", "<p>Body</p>"));
    }

    [Test]
    public void SendBulkAsync_SmtpHostPlaceholder_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns("${SMTP_HOST}");
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        var recipients = new List<string> { "x@test.com", "y@test.com", "z@test.com" };

        Assert.DoesNotThrowAsync(
            async () => await service.SendBulkAsync(recipients, "Campaign", "<p>Content</p>"));
    }

    // ---------------------------------------------------------------
    // SendBulkAsync — with personalizeBody, SMTP not configured
    // ---------------------------------------------------------------

    [Test]
    public void SendBulkAsync_WithPersonalizeBody_SmtpNotConfigured_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        var recipients = new List<string> { "a@test.com", "b@test.com" };

        Assert.DoesNotThrowAsync(
            async () => await service.SendBulkAsync(
                recipients,
                "Personalized",
                "<p>Default body</p>",
                email => $"<p>Hello {email}</p>"));
    }

    // ---------------------------------------------------------------
    // Bogus-generated data for fuzz-style validation
    // ---------------------------------------------------------------

    [Test]
    public void SendAsync_BogusData_SmtpNotConfigured_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        var to = _faker.Internet.Email();
        var subject = _faker.Lorem.Sentence();
        var body = $"<p>{_faker.Lorem.Paragraphs(2)}</p>";

        Assert.DoesNotThrowAsync(
            async () => await service.SendAsync(to, subject, body));
    }

    [Test]
    public void SendBulkAsync_BogusRecipients_SmtpNotConfigured_CompletesWithoutError()
    {
        _mockConfiguration.Setup(c => c["Smtp:Host"]).Returns((string?)null);
        var service = new EmailService(_mockConfiguration.Object, Mock.Of<ILogger<EmailService>>());

        var recipients = Enumerable.Range(0, 5).Select(_ => _faker.Internet.Email()).ToList();
        var subject = _faker.Lorem.Sentence();
        var body = $"<p>{_faker.Lorem.Paragraph()}</p>";

        Assert.DoesNotThrowAsync(
            async () => await service.SendBulkAsync(recipients, subject, body));
    }
}
