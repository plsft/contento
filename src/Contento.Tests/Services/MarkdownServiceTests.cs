using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="MarkdownService"/>. This service has no external dependencies
/// (no IDbConnection, no Redis) — it wraps Markdig pipelines, so all tests exercise
/// the real rendering logic directly.
/// </summary>
[TestFixture]
public class MarkdownServiceTests
{
    private MarkdownService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _service = new MarkdownService(Mock.Of<ILogger<MarkdownService>>());
    }

    // ---------------------------------------------------------------
    // RenderToHtml
    // ---------------------------------------------------------------

    [Test]
    public void RenderToHtml_NullInput_ReturnsEmptyString()
    {
        var result = _service.RenderToHtml(null!);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RenderToHtml_EmptyString_ReturnsEmptyString()
    {
        var result = _service.RenderToHtml(string.Empty);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RenderToHtml_WhitespaceOnly_ReturnsEmptyString()
    {
        var result = _service.RenderToHtml("   \t\n  ");

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RenderToHtml_SimpleParagraph_ReturnsParagraphHtml()
    {
        var result = _service.RenderToHtml("Hello world");

        Assert.That(result.Trim(), Is.EqualTo("<p>Hello world</p>"));
    }

    [Test]
    public void RenderToHtml_BoldText_ReturnsStrongTag()
    {
        var result = _service.RenderToHtml("This is **bold** text");

        Assert.That(result, Does.Contain("<strong>bold</strong>"));
    }

    [Test]
    public void RenderToHtml_ItalicText_ReturnsEmTag()
    {
        var result = _service.RenderToHtml("This is *italic* text");

        Assert.That(result, Does.Contain("<em>italic</em>"));
    }

    [Test]
    public void RenderToHtml_Heading_ReturnsHeadingTag()
    {
        var result = _service.RenderToHtml("# Title");

        Assert.That(result, Does.Contain("<h1"));
        Assert.That(result, Does.Contain("Title"));
    }

    [Test]
    public void RenderToHtml_CodeBlock_ReturnsCodeTag()
    {
        var markdown = "```csharp\nvar x = 1;\n```";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("<code"));
        Assert.That(result, Does.Contain("var x = 1;"));
    }

    [Test]
    public void RenderToHtml_InlineCode_ReturnsCodeTag()
    {
        var result = _service.RenderToHtml("Use the `Console.WriteLine` method");

        Assert.That(result, Does.Contain("<code>Console.WriteLine</code>"));
    }

    [Test]
    public void RenderToHtml_Link_ReturnsAnchorTag()
    {
        var result = _service.RenderToHtml("[Click here](https://example.com)");

        Assert.That(result, Does.Contain("<a"));
        Assert.That(result, Does.Contain("href=\"https://example.com\""));
        Assert.That(result, Does.Contain("Click here"));
    }

    [Test]
    public void RenderToHtml_UnorderedList_ReturnsUlTags()
    {
        var markdown = "- Item one\n- Item two\n- Item three";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("<ul>"));
        Assert.That(result, Does.Contain("<li>Item one</li>"));
        Assert.That(result, Does.Contain("<li>Item two</li>"));
    }

    [Test]
    public void RenderToHtml_Table_ReturnsTableTags()
    {
        var markdown = "| Name | Age |\n|------|-----|\n| Alice | 30 |";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("<table"));
        Assert.That(result, Does.Contain("<th>"));
        Assert.That(result, Does.Contain("Alice"));
    }

    [Test]
    public void RenderToHtml_TaskList_ReturnsCheckboxInputs()
    {
        var markdown = "- [x] Done\n- [ ] Not done";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("<input"));
        Assert.That(result, Does.Contain("type=\"checkbox\""));
    }

    [Test]
    public void RenderToHtml_Callout_ReturnsCalloutDiv()
    {
        var markdown = "> [!note]\n> This is important";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("callout-note"));
        Assert.That(result, Does.Contain("Note"));
    }

    [Test]
    public void RenderToHtml_WarningCallout_ReturnsWarningDiv()
    {
        var markdown = "> [!warning]\n> Be careful";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("callout-warning"));
        Assert.That(result, Does.Contain("Warning"));
    }

    [Test]
    public void RenderToHtml_TipCallout_ReturnsTipDiv()
    {
        var markdown = "> [!tip]\n> Here is a tip";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("callout-tip"));
        Assert.That(result, Does.Contain("Tip"));
    }

    [TestCase("note")]
    [TestCase("warning")]
    [TestCase("tip")]
    [TestCase("info")]
    [TestCase("danger")]
    [TestCase("caution")]
    public void RenderToHtml_AllCalloutTypes_ProduceCalloutDiv(string type)
    {
        var markdown = $"> [!{type}]\n> Callout body";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain($"callout-{type}"));
    }

    [Test]
    public void RenderToHtml_Footnote_RendersFootnoteMarkup()
    {
        var markdown = "Some text[^1]\n\n[^1]: A footnote";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("footnote"));
    }

    [Test]
    public void RenderToHtml_AutoLink_ConvertsUrlToAnchor()
    {
        var result = _service.RenderToHtml("Visit https://example.com for details");

        Assert.That(result, Does.Contain("<a"));
        Assert.That(result, Does.Contain("https://example.com"));
    }

    [Test]
    public void RenderToHtml_Image_ReturnsImgTag()
    {
        var result = _service.RenderToHtml("![Alt text](https://example.com/image.png)");

        Assert.That(result, Does.Contain("<img"));
        Assert.That(result, Does.Contain("Alt text"));
    }

    [Test]
    public void RenderToHtml_MultipleHeadings_RendersAllLevels()
    {
        var markdown = "# H1\n## H2\n### H3\n#### H4";
        var result = _service.RenderToHtml(markdown);

        Assert.That(result, Does.Contain("<h1"));
        Assert.That(result, Does.Contain("<h2"));
        Assert.That(result, Does.Contain("<h3"));
        Assert.That(result, Does.Contain("<h4"));
    }

    // ---------------------------------------------------------------
    // RenderWithTableOfContents
    // ---------------------------------------------------------------

    [Test]
    public void RenderWithTableOfContents_NullInput_ReturnsEmptyHtmlAndNoHeadings()
    {
        var (html, headings) = _service.RenderWithTableOfContents(null!);

        Assert.That(html, Is.EqualTo(string.Empty));
        Assert.That(headings, Is.Empty);
    }

    [Test]
    public void RenderWithTableOfContents_EmptyString_ReturnsEmptyHtmlAndNoHeadings()
    {
        var (html, headings) = _service.RenderWithTableOfContents(string.Empty);

        Assert.That(html, Is.EqualTo(string.Empty));
        Assert.That(headings, Is.Empty);
    }

    [Test]
    public void RenderWithTableOfContents_WhitespaceOnly_ReturnsEmptyHtmlAndNoHeadings()
    {
        var (html, headings) = _service.RenderWithTableOfContents("   ");

        Assert.That(html, Is.EqualTo(string.Empty));
        Assert.That(headings, Is.Empty);
    }

    [Test]
    public void RenderWithTableOfContents_SingleHeading_ReturnsOneHeading()
    {
        var (html, headings) = _service.RenderWithTableOfContents("# Introduction");

        Assert.That(html, Is.Not.Empty);
        var headingList = headings.ToList();
        Assert.That(headingList, Has.Count.EqualTo(1));
        Assert.That(headingList[0].Text, Is.EqualTo("Introduction"));
        Assert.That(headingList[0].Level, Is.EqualTo(1));
        Assert.That(headingList[0].Id, Is.EqualTo("introduction"));
    }

    [Test]
    public void RenderWithTableOfContents_MultipleHeadings_ReturnsAllWithCorrectLevels()
    {
        var markdown = "# Overview\n\n## Getting Started\n\n### Installation\n\n## Usage";
        var (html, headings) = _service.RenderWithTableOfContents(markdown);

        Assert.That(html, Is.Not.Empty);
        var headingList = headings.ToList();
        Assert.That(headingList, Has.Count.EqualTo(4));

        Assert.That(headingList[0].Text, Is.EqualTo("Overview"));
        Assert.That(headingList[0].Level, Is.EqualTo(1));

        Assert.That(headingList[1].Text, Is.EqualTo("Getting Started"));
        Assert.That(headingList[1].Level, Is.EqualTo(2));

        Assert.That(headingList[2].Text, Is.EqualTo("Installation"));
        Assert.That(headingList[2].Level, Is.EqualTo(3));

        Assert.That(headingList[3].Text, Is.EqualTo("Usage"));
        Assert.That(headingList[3].Level, Is.EqualTo(2));
    }

    [Test]
    public void RenderWithTableOfContents_HeadingIdGeneration_StripsSpecialChars()
    {
        var (_, headings) = _service.RenderWithTableOfContents("## What's New in v2.0?");

        var headingList = headings.ToList();
        Assert.That(headingList, Has.Count.EqualTo(1));
        // The ID generation strips non-alphanumeric/space/hyphen, lowercases, replaces spaces with hyphens
        Assert.That(headingList[0].Id, Does.Not.Contain("'"));
        Assert.That(headingList[0].Id, Does.Not.Contain("?"));
        Assert.That(headingList[0].Id, Does.Match(@"^[a-z0-9-]+$"));
    }

    [Test]
    public void RenderWithTableOfContents_NoHeadings_ReturnsHtmlWithEmptyHeadingsList()
    {
        var (html, headings) = _service.RenderWithTableOfContents("Just a paragraph with no headings.");

        Assert.That(html, Is.Not.Empty);
        Assert.That(headings, Is.Empty);
    }

    [Test]
    public void RenderWithTableOfContents_HtmlContainsRenderedContent()
    {
        var markdown = "# Title\n\nSome body text with **bold**.";
        var (html, _) = _service.RenderWithTableOfContents(markdown);

        Assert.That(html, Does.Contain("<h1"));
        Assert.That(html, Does.Contain("<strong>bold</strong>"));
    }

    // ---------------------------------------------------------------
    // CalculateReadingTime
    // ---------------------------------------------------------------

    [Test]
    public void CalculateReadingTime_NullInput_ReturnsMinimumOneMinute()
    {
        var result = _service.CalculateReadingTime(null!);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void CalculateReadingTime_EmptyString_ReturnsMinimumOneMinute()
    {
        var result = _service.CalculateReadingTime(string.Empty);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void CalculateReadingTime_ShortText_ReturnsOneMinute()
    {
        // A few words should still be at least 1 minute
        var result = _service.CalculateReadingTime("Hello world this is a short post.");

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void CalculateReadingTime_200Words_ReturnsOneMinute()
    {
        // Exactly 200 words at 200 WPM = 1 minute
        var words = string.Join(" ", Enumerable.Repeat("word", 200));
        var result = _service.CalculateReadingTime(words);

        Assert.That(result, Is.EqualTo(1));
    }

    [Test]
    public void CalculateReadingTime_201Words_ReturnsTwoMinutes()
    {
        // 201 words at 200 WPM = 1.005 minutes, ceiling rounds up to 2
        var words = string.Join(" ", Enumerable.Repeat("word", 201));
        var result = _service.CalculateReadingTime(words);

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void CalculateReadingTime_1000Words_ReturnsFiveMinutes()
    {
        var words = string.Join(" ", Enumerable.Repeat("word", 1000));
        var result = _service.CalculateReadingTime(words);

        Assert.That(result, Is.EqualTo(5));
    }

    [Test]
    public void CalculateReadingTime_MarkdownSyntaxIsStripped_NotCounted()
    {
        // Heading markers, bold markers, etc. should not inflate the word count
        var markdown = "# Title\n\n**Bold** text with [a link](https://example.com).\n\n```\ncode block\n```";
        var result = _service.CalculateReadingTime(markdown);

        Assert.That(result, Is.GreaterThanOrEqualTo(1));
    }

    // ---------------------------------------------------------------
    // CalculateWordCount
    // ---------------------------------------------------------------

    [Test]
    public void CalculateWordCount_NullInput_ReturnsZero()
    {
        var result = _service.CalculateWordCount(null!);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculateWordCount_EmptyString_ReturnsZero()
    {
        var result = _service.CalculateWordCount(string.Empty);

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculateWordCount_WhitespaceOnly_ReturnsZero()
    {
        var result = _service.CalculateWordCount("   \t\n  ");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void CalculateWordCount_SimpleText_ReturnsCorrectCount()
    {
        var result = _service.CalculateWordCount("The quick brown fox jumps over the lazy dog");

        Assert.That(result, Is.EqualTo(9));
    }

    [Test]
    public void CalculateWordCount_MarkdownHeading_CountsOnlyWords()
    {
        // "# Hello World" should count as 2 words, not 3 (the # is stripped)
        var result = _service.CalculateWordCount("# Hello World");

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void CalculateWordCount_BoldAndItalic_StripsMarkers()
    {
        var result = _service.CalculateWordCount("This is **very** important *text* here");

        Assert.That(result, Is.EqualTo(6));
    }

    [Test]
    public void CalculateWordCount_CodeBlocksRemoved()
    {
        var markdown = "Before code.\n\n```\nfunction foo() {}\n```\n\nAfter code.";
        var result = _service.CalculateWordCount(markdown);

        // "Before code." = 2 words, "After code." = 2 words, code block stripped
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void CalculateWordCount_LinkTextKept_UrlRemoved()
    {
        var result = _service.CalculateWordCount("Click [this link](https://example.com) now");

        // "Click" + "this link" + "now" = 4 words
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void CalculateWordCount_ImageRemoved()
    {
        var result = _service.CalculateWordCount("Text before ![alt](https://img.com/pic.png) text after");

        // "Text before" + "text after" = 4 words
        Assert.That(result, Is.EqualTo(4));
    }

    [Test]
    public void CalculateWordCount_BlockquoteMarkersStripped()
    {
        var result = _service.CalculateWordCount("> This is a quote");

        Assert.That(result, Is.EqualTo(4));
    }

    // ---------------------------------------------------------------
    // GenerateExcerpt
    // ---------------------------------------------------------------

    [Test]
    public void GenerateExcerpt_NullInput_ReturnsEmptyString()
    {
        var result = _service.GenerateExcerpt(null!);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GenerateExcerpt_EmptyString_ReturnsEmptyString()
    {
        var result = _service.GenerateExcerpt(string.Empty);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GenerateExcerpt_ShortText_ReturnsFullText()
    {
        var result = _service.GenerateExcerpt("Short text.");

        Assert.That(result, Is.EqualTo("Short text."));
    }

    [Test]
    public void GenerateExcerpt_TextWithinDefaultMaxLength_ReturnsFullText()
    {
        var text = "A short paragraph that is well under three hundred characters.";
        var result = _service.GenerateExcerpt(text);

        Assert.That(result, Is.EqualTo(text));
    }

    [Test]
    public void GenerateExcerpt_LongText_TruncatesAtWordBoundaryWithEllipsis()
    {
        // Generate a string longer than 300 characters
        var longText = string.Join(" ", Enumerable.Repeat("word", 100));
        var result = _service.GenerateExcerpt(longText);

        Assert.That(result.Length, Is.LessThanOrEqualTo(303)); // 300 + "..."
        Assert.That(result, Does.EndWith("..."));
        Assert.That(result, Does.Not.EndWith(" ..."));
    }

    [Test]
    public void GenerateExcerpt_CustomMaxLength_RespectsLimit()
    {
        var longText = string.Join(" ", Enumerable.Repeat("testing", 50));
        var result = _service.GenerateExcerpt(longText, maxLength: 50);

        Assert.That(result.Length, Is.LessThanOrEqualTo(53)); // 50 + "..."
        Assert.That(result, Does.EndWith("..."));
    }

    [Test]
    public void GenerateExcerpt_TextExactlyAtMaxLength_ReturnsWithoutEllipsis()
    {
        // Create text that is exactly 300 chars
        var text = new string('A', 300);
        var result = _service.GenerateExcerpt(text);

        Assert.That(result, Is.EqualTo(text));
        Assert.That(result, Does.Not.EndWith("..."));
    }

    [Test]
    public void GenerateExcerpt_StripsMarkdownSyntax()
    {
        var markdown = "# My Title\n\nThis is a **paragraph** with [a link](https://example.com).";
        var result = _service.GenerateExcerpt(markdown);

        Assert.That(result, Does.Not.Contain("#"));
        Assert.That(result, Does.Not.Contain("**"));
        Assert.That(result, Does.Not.Contain("]("));
    }

    [Test]
    public void GenerateExcerpt_WhitespaceOnly_ReturnsEmptyString()
    {
        var result = _service.GenerateExcerpt("   \n\t  ");

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    // ---------------------------------------------------------------
    // RenderCommentToHtml
    // ---------------------------------------------------------------

    [Test]
    public void RenderCommentToHtml_NullInput_ReturnsEmptyString()
    {
        var result = _service.RenderCommentToHtml(null!);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RenderCommentToHtml_EmptyString_ReturnsEmptyString()
    {
        var result = _service.RenderCommentToHtml(string.Empty);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void RenderCommentToHtml_SimpleParagraph_ReturnsParagraphHtml()
    {
        var result = _service.RenderCommentToHtml("Great post!");

        Assert.That(result.Trim(), Is.EqualTo("<p>Great post!</p>"));
    }

    [Test]
    public void RenderCommentToHtml_Bold_ReturnsStrongTag()
    {
        var result = _service.RenderCommentToHtml("This is **important**");

        Assert.That(result, Does.Contain("<strong>important</strong>"));
    }

    [Test]
    public void RenderCommentToHtml_Italic_ReturnsEmTag()
    {
        var result = _service.RenderCommentToHtml("This is *emphasized*");

        Assert.That(result, Does.Contain("<em>emphasized</em>"));
    }

    [Test]
    public void RenderCommentToHtml_InlineCode_ReturnsCodeTag()
    {
        var result = _service.RenderCommentToHtml("Use `var x = 1` here");

        Assert.That(result, Does.Contain("<code>var x = 1</code>"));
    }

    [Test]
    public void RenderCommentToHtml_AutoLink_ConvertsToAnchor()
    {
        var result = _service.RenderCommentToHtml("Check https://example.com");

        Assert.That(result, Does.Contain("<a"));
        Assert.That(result, Does.Contain("https://example.com"));
    }

    [Test]
    public void RenderCommentToHtml_StrictMode_DoesNotRenderAdvancedExtensions()
    {
        // The comment pipeline only uses AutoLinks; it should not support task lists,
        // footnotes, or other advanced features
        var result = _service.RenderCommentToHtml("- [x] Done task");

        // Should render as a regular list item, not a checkbox
        Assert.That(result, Does.Not.Contain("type=\"checkbox\""));
    }

    [Test]
    public void RenderCommentToHtml_WhitespaceOnly_ReturnsEmptyString()
    {
        var result = _service.RenderCommentToHtml("   \n\t  ");

        Assert.That(result, Is.EqualTo(string.Empty));
    }
}
