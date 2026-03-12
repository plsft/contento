using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="ShortcodeProcessor"/>. Validates constructor guards,
/// built-in shortcode expansion, custom registration, and pass-through of
/// unknown shortcodes.
/// </summary>
[TestFixture]
public class ShortcodeProcessorTests
{
    private ShortcodeProcessor _processor = null!;

    [SetUp]
    public void SetUp()
    {
        _processor = new ShortcodeProcessor(Mock.Of<ILogger<ShortcodeProcessor>>());
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(
            () => new ShortcodeProcessor(null!));
    }

    // ---------------------------------------------------------------
    // Process — null/empty/no-shortcode handling
    // ---------------------------------------------------------------

    [Test]
    public void Process_NullContent_ReturnsEmpty()
    {
        var result = _processor.Process(null!);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Process_EmptyContent_ReturnsEmpty()
    {
        var result = _processor.Process(string.Empty);
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void Process_NoShortcodes_ReturnsUnchanged()
    {
        var content = "<p>Hello world, no shortcodes here.</p>";
        var result = _processor.Process(content);
        Assert.That(result, Is.EqualTo(content));
    }

    // ---------------------------------------------------------------
    // Built-in shortcodes
    // ---------------------------------------------------------------

    [Test]
    public void Process_YoutubeShortcode_GeneratesIframe()
    {
        var result = _processor.Process("[youtube id=\"dQw4w9WgXcQ\"]");

        Assert.That(result, Does.Contain("<iframe"));
        Assert.That(result, Does.Contain("youtube.com/embed/dQw4w9WgXcQ"));
        Assert.That(result, Does.Contain("allowfullscreen"));
        Assert.That(result, Does.Contain("loading=\"lazy\""));
        Assert.That(result, Does.Contain("class=\"oembed-embed\""));
    }

    [Test]
    public void Process_VimeoShortcode_GeneratesIframe()
    {
        var result = _processor.Process("[vimeo id=\"123456\"]");

        Assert.That(result, Does.Contain("<iframe"));
        Assert.That(result, Does.Contain("player.vimeo.com/video/123456"));
        Assert.That(result, Does.Contain("allowfullscreen"));
        Assert.That(result, Does.Contain("loading=\"lazy\""));
        Assert.That(result, Does.Contain("class=\"oembed-embed\""));
    }

    [Test]
    public void Process_ButtonShortcode_GeneratesLink()
    {
        var result = _processor.Process("[button url=\"https://example.com\" text=\"Click Me\" style=\"primary\"]");

        Assert.That(result, Does.Contain("<a href=\"https://example.com\""));
        Assert.That(result, Does.Contain("class=\"btn btn-primary\""));
        Assert.That(result, Does.Contain("Click Me"));
    }

    [Test]
    public void Process_CalloutShortcode_GeneratesDiv()
    {
        var result = _processor.Process("[callout type=\"warning\" title=\"Heads Up\"]Be careful here.[/callout]");

        Assert.That(result, Does.Contain("class=\"callout callout-warning\""));
        Assert.That(result, Does.Contain("class=\"callout-title\""));
        Assert.That(result, Does.Contain("Heads Up"));
        Assert.That(result, Does.Contain("Be careful here."));
    }

    [Test]
    public void Process_CodeShortcode_GeneratesPreCode()
    {
        var result = _processor.Process("[code lang=\"javascript\"]console.log('hello');[/code]");

        Assert.That(result, Does.Contain("<pre>"));
        Assert.That(result, Does.Contain("<code class=\"language-javascript\">"));
        Assert.That(result, Does.Contain("console.log("));
    }

    [Test]
    public void Process_TocShortcode_GeneratesPlaceholder()
    {
        var result = _processor.Process("[toc]");

        Assert.That(result, Does.Contain("class=\"table-of-contents\""));
        Assert.That(result, Does.Contain("id=\"toc\""));
    }

    [Test]
    public void Process_GalleryShortcode_GeneratesImages()
    {
        var result = _processor.Process("[gallery ids=\"abc,def,ghi\" columns=\"3\"]");

        Assert.That(result, Does.Contain("class=\"gallery gallery-columns-3\""));
        Assert.That(result, Does.Contain("/api/v1/media/abc/file"));
        Assert.That(result, Does.Contain("/api/v1/media/def/file"));
        Assert.That(result, Does.Contain("/api/v1/media/ghi/file"));
    }

    // ---------------------------------------------------------------
    // Unknown shortcodes
    // ---------------------------------------------------------------

    [Test]
    public void Process_UnknownShortcode_LeftUnchanged()
    {
        var content = "[nonexistent foo=\"bar\"]";
        var result = _processor.Process(content);
        Assert.That(result, Is.EqualTo(content));
    }

    // ---------------------------------------------------------------
    // Multiple shortcodes
    // ---------------------------------------------------------------

    [Test]
    public void Process_MultipleShortcodes_AllProcessed()
    {
        var content = "Before [youtube id=\"abc\"] middle [toc] after";
        var result = _processor.Process(content);

        Assert.That(result, Does.Contain("youtube.com/embed/abc"));
        Assert.That(result, Does.Contain("table-of-contents"));
        Assert.That(result, Does.Contain("Before"));
        Assert.That(result, Does.Contain("middle"));
        Assert.That(result, Does.Contain("after"));
    }

    // ---------------------------------------------------------------
    // Custom registration
    // ---------------------------------------------------------------

    [Test]
    public void Register_CustomShortcode_Works()
    {
        _processor.Register("greeting", (attrs, _) =>
        {
            var name = attrs.GetValueOrDefault("name", "World");
            return $"<span class=\"greeting\">Hello, {name}!</span>";
        });

        var result = _processor.Process("[greeting name=\"Claude\"]");

        Assert.That(result, Does.Contain("Hello, Claude!"));
        Assert.That(result, Does.Contain("class=\"greeting\""));
    }

    // ---------------------------------------------------------------
    // Service implements IShortcodeProcessor
    // ---------------------------------------------------------------

    [Test]
    public void Service_ImplementsIShortcodeProcessor()
    {
        Assert.That(_processor, Is.InstanceOf<IShortcodeProcessor>());
    }

    // ---------------------------------------------------------------
    // Button shortcode defaults
    // ---------------------------------------------------------------

    [Test]
    public void Process_ButtonShortcode_DefaultStyle_IsPrimary()
    {
        var result = _processor.Process("[button url=\"/test\" text=\"Go\"]");

        Assert.That(result, Does.Contain("btn btn-primary"));
    }

    // ---------------------------------------------------------------
    // Callout shortcode without title
    // ---------------------------------------------------------------

    [Test]
    public void Process_CalloutShortcode_NoTitle_OmitsTitleParagraph()
    {
        var result = _processor.Process("[callout type=\"info\"]Some info.[/callout]");

        Assert.That(result, Does.Contain("class=\"callout callout-info\""));
        Assert.That(result, Does.Not.Contain("callout-title"));
        Assert.That(result, Does.Contain("Some info."));
    }

    // ---------------------------------------------------------------
    // Code shortcode without lang
    // ---------------------------------------------------------------

    [Test]
    public void Process_CodeShortcode_NoLang_OmitsClassAttribute()
    {
        var result = _processor.Process("[code]plain text[/code]");

        Assert.That(result, Does.Contain("<pre><code>"));
        Assert.That(result, Does.Not.Contain("language-"));
    }
}
