using System.Text.RegularExpressions;
using System.Web;
using Noundry.Guardian;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// Parses and expands shortcode tags in content. Ships with built-in shortcodes
/// for youtube, vimeo, button, callout, gallery, code, and toc. Custom shortcodes
/// can be registered at runtime.
/// </summary>
public class ShortcodeProcessor : IShortcodeProcessor
{
    private readonly ILogger<ShortcodeProcessor> _logger;
    private readonly Dictionary<string, Func<Dictionary<string, string>, string?, string>> _handlers = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex ShortcodePattern = new(
        @"\[([\w-]+)((?:\s+[\w-]+=""[^""]*"")*)\](?:(.*?)\[\/\1\])?",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex AttributePattern = new(
        @"([\w-]+)=""([^""]*)""",
        RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of <see cref="ShortcodeProcessor"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ShortcodeProcessor(ILogger<ShortcodeProcessor> logger)
    {
        _logger = Guard.Against.Null(logger);

        RegisterBuiltInShortcodes();
    }

    /// <inheritdoc />
    public string Process(string content, ShortcodeContext? context = null)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        return ShortcodePattern.Replace(content, match =>
        {
            var name = match.Groups[1].Value;
            var rawAttributes = match.Groups[2].Value;
            var innerContent = match.Groups[3].Success ? match.Groups[3].Value : null;

            if (!_handlers.TryGetValue(name, out var handler))
            {
                // Unknown shortcode — leave unchanged
                return match.Value;
            }

            var attributes = ParseAttributes(rawAttributes);

            try
            {
                return handler(attributes, innerContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Shortcode [{Name}] handler failed", name);
                return match.Value;
            }
        });
    }

    /// <inheritdoc />
    public void Register(string name, Func<Dictionary<string, string>, string?, string> handler)
    {
        Guard.Against.NullOrWhiteSpace(name);
        Guard.Against.Null(handler);

        _handlers[name] = handler;
        _logger.LogInformation("Registered shortcode [{Name}]", name);
    }

    private static Dictionary<string, string> ParseAttributes(string raw)
    {
        var attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(raw))
            return attrs;

        foreach (Match m in AttributePattern.Matches(raw))
        {
            attrs[m.Groups[1].Value] = m.Groups[2].Value;
        }

        return attrs;
    }

    private void RegisterBuiltInShortcodes()
    {
        // [youtube id="VIDEO_ID"]
        Register("youtube", (attrs, _) =>
        {
            var id = attrs.GetValueOrDefault("id", "");
            if (string.IsNullOrEmpty(id)) return "[youtube: missing id]";
            return $"<div class=\"oembed-embed\"><iframe src=\"https://www.youtube.com/embed/{HttpUtility.HtmlAttributeEncode(id)}\" width=\"560\" height=\"315\" frameborder=\"0\" allowfullscreen loading=\"lazy\"></iframe></div>";
        });

        // [vimeo id="VIDEO_ID"]
        Register("vimeo", (attrs, _) =>
        {
            var id = attrs.GetValueOrDefault("id", "");
            if (string.IsNullOrEmpty(id)) return "[vimeo: missing id]";
            return $"<div class=\"oembed-embed\"><iframe src=\"https://player.vimeo.com/video/{HttpUtility.HtmlAttributeEncode(id)}\" width=\"560\" height=\"315\" frameborder=\"0\" allowfullscreen loading=\"lazy\"></iframe></div>";
        });

        // [button url="URL" text="Text" style="primary|secondary"]
        Register("button", (attrs, _) =>
        {
            var url = attrs.GetValueOrDefault("url", "#");
            var text = attrs.GetValueOrDefault("text", "Click here");
            var style = attrs.GetValueOrDefault("style", "primary");
            return $"<a href=\"{HttpUtility.HtmlAttributeEncode(url)}\" class=\"btn btn-{HttpUtility.HtmlAttributeEncode(style)}\">{HttpUtility.HtmlEncode(text)}</a>";
        });

        // [callout type="info|warning|tip" title="Title"]content[/callout]
        Register("callout", (attrs, content) =>
        {
            var type = attrs.GetValueOrDefault("type", "info");
            var title = attrs.GetValueOrDefault("title", "");
            var titleHtml = string.IsNullOrEmpty(title)
                ? ""
                : $"<p class=\"callout-title\">{HttpUtility.HtmlEncode(title)}</p>";
            return $"<div class=\"callout callout-{HttpUtility.HtmlAttributeEncode(type)}\">{titleHtml}{content}</div>";
        });

        // [gallery ids="id1,id2,id3" columns="3"]
        Register("gallery", (attrs, _) =>
        {
            var idsRaw = attrs.GetValueOrDefault("ids", "");
            var columns = attrs.GetValueOrDefault("columns", "3");

            if (string.IsNullOrEmpty(idsRaw)) return "[gallery: missing ids]";

            var ids = idsRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var images = string.Join("", ids.Select(id =>
                $"<img src=\"/api/v1/media/{HttpUtility.HtmlAttributeEncode(id)}/file\" loading=\"lazy\" alt=\"\" />"));

            return $"<div class=\"gallery gallery-columns-{HttpUtility.HtmlAttributeEncode(columns)}\">{images}</div>";
        });

        // [code lang="javascript"]code here[/code]
        Register("code", (attrs, content) =>
        {
            var lang = attrs.GetValueOrDefault("lang", "");
            var langClass = string.IsNullOrEmpty(lang) ? "" : $" class=\"language-{HttpUtility.HtmlAttributeEncode(lang)}\"";
            return $"<pre><code{langClass}>{HttpUtility.HtmlEncode(content ?? "")}</code></pre>";
        });

        // [toc]
        Register("toc", (_, _) =>
        {
            return "<div class=\"table-of-contents\" id=\"toc\"></div>";
        });
    }
}
