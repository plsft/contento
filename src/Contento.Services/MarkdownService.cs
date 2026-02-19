using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// Service for rendering Markdown to HTML using Markdig with GFM extensions,
/// syntax highlighting, custom callout blocks, content metrics, and strict comment rendering.
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly MarkdownPipeline _commentPipeline;
    private readonly ILogger<MarkdownService> _logger;
    private readonly IOEmbedService? _oEmbedService;
    private readonly IShortcodeProcessor? _shortcodeProcessor;
    private const int WordsPerMinute = 200;

    /// <summary>
    /// Initializes a new instance of <see cref="MarkdownService"/> with pre-configured
    /// Markdig pipelines for full post rendering and strict comment rendering.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="oEmbedService">Optional oEmbed service for URL embeds.</param>
    /// <param name="shortcodeProcessor">Optional shortcode processor.</param>
    public MarkdownService(
        ILogger<MarkdownService> logger,
        IOEmbedService? oEmbedService = null,
        IShortcodeProcessor? shortcodeProcessor = null)
    {
        _logger = logger;
        _oEmbedService = oEmbedService;
        _shortcodeProcessor = shortcodeProcessor;
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseAutoIdentifiers()
            .UseTaskLists()
            .UseFootnotes()
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .Build();

        _commentPipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .Build();
    }

    /// <inheritdoc />
    public string RenderToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var processed = ProcessCallouts(markdown);
        var html = Markdown.ToHtml(processed, _pipeline);

        // Process oEmbed URLs (standalone URLs in paragraphs become embeds)
        if (_oEmbedService != null)
        {
            try
            {
                html = _oEmbedService.ProcessContent(html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "oEmbed processing failed, returning raw HTML");
            }
        }

        // Process shortcodes
        if (_shortcodeProcessor != null)
        {
            try
            {
                html = _shortcodeProcessor.Process(html);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shortcode processing failed, returning raw HTML");
            }
        }

        return html;
    }

    /// <inheritdoc />
    public (string Html, IEnumerable<(string Id, string Text, int Level)> Headings) RenderWithTableOfContents(
        string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return (string.Empty, Enumerable.Empty<(string, string, int)>());

        var processed = ProcessCallouts(markdown);
        var document = Markdown.Parse(processed, _pipeline);
        var headings = new List<(string Id, string Text, int Level)>();

        foreach (var heading in document.Descendants<HeadingBlock>())
        {
            var text = ExtractInlineText(heading.Inline);
            var id = GenerateHeadingId(text);
            headings.Add((id, text, heading.Level));
        }

        var html = Markdown.ToHtml(processed, _pipeline);
        return (html, headings);
    }

    /// <inheritdoc />
    public int CalculateReadingTime(string markdown)
    {
        var wordCount = CalculateWordCount(markdown);
        var minutes = (int)Math.Ceiling((double)wordCount / WordsPerMinute);
        return Math.Max(minutes, 1);
    }

    /// <inheritdoc />
    public int CalculateWordCount(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return 0;

        // Strip common Markdown syntax to get plain text for counting
        var text = StripMarkdownSyntax(markdown);
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length;
    }

    /// <inheritdoc />
    public string GenerateExcerpt(string markdown, int maxLength = 300)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        var plainText = StripMarkdownSyntax(markdown).Trim();

        if (plainText.Length <= maxLength)
            return plainText;

        // Truncate at the last word boundary before maxLength
        var truncated = plainText[..maxLength];
        var lastSpace = truncated.LastIndexOf(' ');
        if (lastSpace > 0)
            truncated = truncated[..lastSpace];

        return truncated + "...";
    }

    /// <inheritdoc />
    public string RenderCommentToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return string.Empty;

        return Markdown.ToHtml(markdown, _commentPipeline);
    }

    /// <summary>
    /// Processes custom callout blocks in the format &gt; [!note], &gt; [!warning], &gt; [!tip]
    /// and wraps them in styled HTML div elements.
    /// </summary>
    private static string ProcessCallouts(string markdown)
    {
        // Match blockquotes starting with [!type] pattern
        var calloutPattern = new Regex(
            @"^(>\s*)\[!(note|warning|tip|info|danger|caution)\]\s*\n((?:>.*\n?)*)",
            RegexOptions.Multiline | RegexOptions.IgnoreCase);

        return calloutPattern.Replace(markdown, match =>
        {
            var type = match.Groups[2].Value.ToLowerInvariant();
            var content = match.Groups[3].Value;
            // Strip the leading '> ' from each line of the callout body
            var body = Regex.Replace(content, @"^>\s?", "", RegexOptions.Multiline).Trim();

            return $"<div class=\"callout callout-{type}\">\n<p class=\"callout-title\">{Capitalize(type)}</p>\n\n{body}\n\n</div>\n";
        });
    }

    /// <summary>
    /// Strips common Markdown syntax characters to produce approximate plain text for word counting.
    /// </summary>
    private static string StripMarkdownSyntax(string markdown)
    {
        var text = markdown;

        // Remove code blocks
        text = Regex.Replace(text, @"```[\s\S]*?```", " ");
        text = Regex.Replace(text, @"`[^`]+`", " ");

        // Remove images
        text = Regex.Replace(text, @"!\[.*?\]\(.*?\)", " ");

        // Remove links but keep link text
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^\)]+\)", "$1");

        // Remove heading markers
        text = Regex.Replace(text, @"^#{1,6}\s+", "", RegexOptions.Multiline);

        // Remove emphasis markers
        text = Regex.Replace(text, @"[*_]{1,3}", "");

        // Remove blockquote markers
        text = Regex.Replace(text, @"^>\s*", "", RegexOptions.Multiline);

        // Remove horizontal rules
        text = Regex.Replace(text, @"^[-*_]{3,}\s*$", "", RegexOptions.Multiline);

        // Remove HTML tags
        text = Regex.Replace(text, @"<[^>]+>", " ");

        return text;
    }

    /// <summary>
    /// Extracts the text content from a Markdig inline container.
    /// </summary>
    private static string ExtractInlineText(ContainerInline? inline)
    {
        if (inline == null)
            return string.Empty;

        var text = string.Empty;
        foreach (var child in inline)
        {
            if (child is LiteralInline literal)
                text += literal.Content.ToString();
        }
        return text;
    }

    /// <summary>
    /// Generates a URL-friendly identifier from heading text for anchor links.
    /// </summary>
    private static string GenerateHeadingId(string text)
    {
        var id = text.ToLowerInvariant().Trim();
        id = Regex.Replace(id, @"[^a-z0-9\s-]", "");
        id = Regex.Replace(id, @"[\s]+", "-");
        return id.Trim('-');
    }

    /// <summary>
    /// Capitalizes the first letter of a string.
    /// </summary>
    private static string Capitalize(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        return char.ToUpperInvariant(value[0]) + value[1..];
    }
}
