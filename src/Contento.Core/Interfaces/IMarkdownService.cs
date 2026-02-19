namespace Contento.Core.Interfaces;

/// <summary>
/// Service for rendering Markdown content to HTML using Markdig.
/// Supports GFM, syntax highlighting, custom callouts, embeds, and content metrics.
/// </summary>
public interface IMarkdownService
{
    /// <summary>
    /// Renders a Markdown string to sanitized HTML.
    /// Supports GitHub-Flavored Markdown, syntax-highlighted code blocks,
    /// footnotes, task lists, tables, auto-links, and custom callout blocks.
    /// Output is XSS-sanitized with only allowlisted HTML tags permitted.
    /// </summary>
    /// <param name="markdown">The Markdown source text.</param>
    /// <returns>The rendered and sanitized HTML string.</returns>
    string RenderToHtml(string markdown);

    /// <summary>
    /// Renders a Markdown string to sanitized HTML with heading IDs generated
    /// for table-of-contents linking.
    /// </summary>
    /// <param name="markdown">The Markdown source text.</param>
    /// <returns>A tuple containing the rendered HTML and a collection of headings with their IDs and levels.</returns>
    (string Html, IEnumerable<(string Id, string Text, int Level)> Headings) RenderWithTableOfContents(string markdown);

    /// <summary>
    /// Calculates the estimated reading time in minutes for a Markdown document.
    /// Uses an average reading speed of 200 words per minute.
    /// </summary>
    /// <param name="markdown">The Markdown source text.</param>
    /// <returns>The estimated reading time in minutes (minimum 1).</returns>
    int CalculateReadingTime(string markdown);

    /// <summary>
    /// Counts the number of words in a Markdown document, excluding markup syntax.
    /// </summary>
    /// <param name="markdown">The Markdown source text.</param>
    /// <returns>The total word count.</returns>
    int CalculateWordCount(string markdown);

    /// <summary>
    /// Generates a plain-text excerpt from Markdown content, stripping all markup.
    /// </summary>
    /// <param name="markdown">The Markdown source text.</param>
    /// <param name="maxLength">Maximum length of the excerpt in characters.</param>
    /// <returns>A plain-text excerpt, truncated with ellipsis if needed.</returns>
    string GenerateExcerpt(string markdown, int maxLength = 300);

    /// <summary>
    /// Renders Markdown specifically for comments with a stricter sanitization policy.
    /// Only allows basic formatting: bold, italic, code, links, and lists.
    /// No headings, images, or raw HTML permitted.
    /// </summary>
    /// <param name="markdown">The Markdown source text from a comment.</param>
    /// <returns>The rendered and strictly sanitized HTML string.</returns>
    string RenderCommentToHtml(string markdown);
}
