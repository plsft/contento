using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Noundry.AIG.Client;
using Noundry.AIG.Core.Models;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Services;

namespace Contento.Tests.Services;

[TestFixture]
public class AiServiceTests
{
    private Mock<IAigClient> _mockAigClient = null!;
    private Mock<ILogger<AiService>> _mockLogger = null!;
    private AiService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockAigClient = new Mock<IAigClient>();
        _mockLogger = new Mock<ILogger<AiService>>();
        _service = new AiService(_mockAigClient.Object, _mockLogger.Object);
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullAigClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AiService(null!, _mockLogger.Object));
    }

    [Test]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new AiService(_mockAigClient.Object, null!));
    }

    [Test]
    public void Constructor_ValidDependencies_CreatesInstance()
    {
        var service = new AiService(_mockAigClient.Object, _mockLogger.Object);
        Assert.That(service, Is.Not.Null);
    }

    // ---------------------------------------------------------------
    // CompleteAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task CompleteAsync_EmptyApiKey_ReturnsError()
    {
        var settings = new AiSettings { ApiKey = "", Enabled = true };
        var result = await _service.CompleteAsync("system", "user", settings);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("API key"));
        });
    }

    [Test]
    public async Task CompleteAsync_WhitespaceApiKey_ReturnsError()
    {
        var settings = new AiSettings { ApiKey = "   ", Enabled = true };
        var result = await _service.CompleteAsync("system", "user", settings);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CompleteAsync_ValidResponse_ReturnsText()
    {
        var response = new AiResponse
        {
            Choices = new List<Choice>
            {
                new() { Message = new Message { Content = "Hello world", Role = "assistant" } }
            },
            Usage = new Usage { TotalTokens = 42 }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-test123", Model = "openai/gpt-4o-mini", Enabled = true };
        var result = await _service.CompleteAsync("system", "user", settings);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Text, Is.EqualTo("Hello world"));
        });
    }

    [Test]
    public async Task CompleteAsync_ErrorResponse_ReturnsError()
    {
        var response = new AiResponse
        {
            Error = new AiError { Message = "Invalid API key" }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-bad", Model = "openai/gpt-4o-mini", Enabled = true };
        var result = await _service.CompleteAsync("system", "user", settings);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("Invalid API key"));
        });
    }

    [Test]
    public async Task CompleteAsync_AigClientThrows_ReturnsError()
    {
        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        var settings = new AiSettings { ApiKey = "sk-test", Model = "openai/gpt-4o-mini", Enabled = true };
        var result = await _service.CompleteAsync("system", "user", settings);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Does.Contain("Network error"));
        });
    }

    // ---------------------------------------------------------------
    // GenerateThemeAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task GenerateThemeAsync_ValidJson_ReturnsTheme()
    {
        var themeJson = """
        {
            "name": "Ocean Breeze",
            "slug": "ocean-breeze",
            "description": "A calming ocean theme",
            "version": "1.0.0",
            "author": "AI Generated",
            "cssVariables": {
                "--font-body": "'Inter', system-ui, sans-serif",
                "--color-accent": "#0077B6",
                "--color-bg": "#F0F8FF",
                "--color-text": "#1B2838"
            }
        }
        """;

        var response = new AiResponse
        {
            Choices = new List<Choice>
            {
                new() { Message = new Message { Content = themeJson, Role = "assistant" } }
            }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-test", Model = "openai/gpt-4o-mini", Enabled = true };
        var theme = await _service.GenerateThemeAsync("ocean theme", settings);

        Assert.That(theme, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(theme!.Name, Is.EqualTo("Ocean Breeze"));
            Assert.That(theme.Slug, Is.EqualTo("ocean-breeze"));
            Assert.That(theme.CssVariables, Does.Contain("--font-body"));
            Assert.That(theme.CssVariables, Does.Contain("#0077B6"));
        });
    }

    [Test]
    public async Task GenerateThemeAsync_JsonInCodeFence_ParsesCorrectly()
    {
        var responseText = """
        Here's a theme for you:

        ```json
        {
            "name": "Sunset",
            "slug": "sunset",
            "description": "Warm sunset colors",
            "cssVariables": {
                "--font-body": "'Lora', serif",
                "--color-accent": "#FF6B35",
                "--color-bg": "#FFF5E6",
                "--color-text": "#2D1810"
            }
        }
        ```
        """;

        var response = new AiResponse
        {
            Choices = new List<Choice>
            {
                new() { Message = new Message { Content = responseText, Role = "assistant" } }
            }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-test", Model = "openai/gpt-4o-mini", Enabled = true };
        var theme = await _service.GenerateThemeAsync("sunset theme", settings);

        Assert.That(theme, Is.Not.Null);
        Assert.That(theme!.Name, Is.EqualTo("Sunset"));
    }

    [Test]
    public async Task GenerateThemeAsync_MalformedJson_ReturnsNull()
    {
        var response = new AiResponse
        {
            Choices = new List<Choice>
            {
                new() { Message = new Message { Content = "This is not valid JSON at all", Role = "assistant" } }
            }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-test", Model = "openai/gpt-4o-mini", Enabled = true };
        var theme = await _service.GenerateThemeAsync("bad prompt", settings);

        Assert.That(theme, Is.Null);
    }

    [Test]
    public async Task GenerateThemeAsync_EmptyApiKey_ReturnsNull()
    {
        var settings = new AiSettings { ApiKey = "", Enabled = true };
        var theme = await _service.GenerateThemeAsync("any prompt", settings);
        Assert.That(theme, Is.Null);
    }

    // ---------------------------------------------------------------
    // GenerateLayoutAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task GenerateLayoutAsync_ValidJson_ReturnsLayout()
    {
        var layoutJson = """
        {
            "name": "Blog Classic",
            "slug": "blog-classic",
            "description": "Classic two-column blog layout",
            "structure": {
                "grid": "12-col",
                "rows": [
                    { "regions": [{ "region": "header", "cols": 12 }] },
                    { "regions": [{ "region": "body", "cols": 8 }, { "region": "sidebar", "cols": 4 }] },
                    { "regions": [{ "region": "footer", "cols": 12 }] }
                ],
                "defaults": { "gap": "md", "maxWidth": "1280px", "padding": "lg" }
            },
            "customCss": "",
            "headContent": ""
        }
        """;

        var response = new AiResponse
        {
            Choices = new List<Choice>
            {
                new() { Message = new Message { Content = layoutJson, Role = "assistant" } }
            }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-test", Model = "openai/gpt-4o-mini", Enabled = true };
        var siteId = Guid.NewGuid();
        var layout = await _service.GenerateLayoutAsync("classic blog", siteId, settings);

        Assert.That(layout, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(layout!.Name, Is.EqualTo("Blog Classic"));
            Assert.That(layout.Slug, Is.EqualTo("blog-classic"));
            Assert.That(layout.SiteId, Is.EqualTo(siteId));
            Assert.That(layout.Structure, Does.Contain("12-col"));
            Assert.That(layout.Structure, Does.Contain("header"));
        });
    }

    [Test]
    public async Task GenerateLayoutAsync_MalformedJson_ReturnsNull()
    {
        var response = new AiResponse
        {
            Choices = new List<Choice>
            {
                new() { Message = new Message { Content = "not json", Role = "assistant" } }
            }
        };

        _mockAigClient
            .Setup(c => c.SendAsync(It.IsAny<AiRequest>(), It.IsAny<ProviderConfig>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var settings = new AiSettings { ApiKey = "sk-test", Model = "openai/gpt-4o-mini", Enabled = true };
        var layout = await _service.GenerateLayoutAsync("bad prompt", Guid.NewGuid(), settings);

        Assert.That(layout, Is.Null);
    }

    [Test]
    public async Task GenerateLayoutAsync_EmptyApiKey_ReturnsNull()
    {
        var settings = new AiSettings { ApiKey = "", Enabled = true };
        var layout = await _service.GenerateLayoutAsync("any prompt", Guid.NewGuid(), settings);
        Assert.That(layout, Is.Null);
    }

    // ---------------------------------------------------------------
    // StreamAsync
    // ---------------------------------------------------------------

    [Test]
    public async Task StreamAsync_EmptyApiKey_YieldsErrorMessage()
    {
        var settings = new AiSettings { ApiKey = "", Enabled = true };
        var chunks = new List<string>();

        await foreach (var chunk in _service.StreamAsync("system", "user", settings))
        {
            chunks.Add(chunk);
        }

        Assert.That(chunks, Has.Count.EqualTo(1));
        Assert.That(chunks[0], Does.Contain("API key"));
    }

    // ---------------------------------------------------------------
    // AiSettings model
    // ---------------------------------------------------------------

    [Test]
    public void AiSettings_Defaults_AreCorrect()
    {
        var settings = new AiSettings();
        Assert.Multiple(() =>
        {
            Assert.That(settings.Provider, Is.EqualTo("openai"));
            Assert.That(settings.Model, Is.EqualTo("openai/gpt-4o-mini"));
            Assert.That(settings.ApiKey, Is.EqualTo(""));
            Assert.That(settings.Enabled, Is.False);
        });
    }

    // ---------------------------------------------------------------
    // AiCompletionResult model
    // ---------------------------------------------------------------

    [Test]
    public void AiCompletionResult_Defaults_AreCorrect()
    {
        var result = new AiCompletionResult();
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Text, Is.EqualTo(""));
            Assert.That(result.Error, Is.Null);
            Assert.That(result.TokensUsed, Is.EqualTo(0));
        });
    }
}
