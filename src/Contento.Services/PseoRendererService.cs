using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Renders pSEO pages into full HTML output with project chrome, JSON-LD structured data,
/// and schema-driven content rendering.
/// </summary>
public class PseoRendererService : IPseoRendererService
{
    private readonly IContentSchemaService _schemaService;
    private readonly ILogger<PseoRendererService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PseoRendererService"/>.
    /// </summary>
    /// <param name="schemaService">The content schema service.</param>
    /// <param name="logger">The logger.</param>
    public PseoRendererService(IContentSchemaService schemaService, ILogger<PseoRendererService> logger)
    {
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public Task<string> RenderPageAsync(PseoPage page, PseoProject project, ContentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(schema);

        var sb = new StringBuilder(8192);
        var canonicalUrl = $"https://{Encode(project.Fqdn)}/{Encode(page.Slug)}";
        var metaDescription = Encode(page.MetaDescription ?? "");
        var encodedTitle = Encode(page.Title);

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("  <meta charset=\"utf-8\">");
        sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.AppendLine($"  <title>{encodedTitle}</title>");
        sb.AppendLine($"  <meta name=\"description\" content=\"{metaDescription}\">");
        sb.AppendLine($"  <link rel=\"canonical\" href=\"{canonicalUrl}\">");
        sb.AppendLine($"  <meta property=\"og:title\" content=\"{encodedTitle}\">");
        sb.AppendLine($"  <meta property=\"og:description\" content=\"{metaDescription}\">");
        sb.AppendLine($"  <meta property=\"og:url\" content=\"{canonicalUrl}\">");
        sb.AppendLine("  <meta property=\"og:type\" content=\"article\">");

        // Custom CSS
        if (!string.IsNullOrWhiteSpace(project.CustomCss))
        {
            sb.AppendLine($"  <style>{project.CustomCss}</style>");
        }

        // JSON-LD structured data
        AppendArticleJsonLd(sb, page, project);
        AppendBreadcrumbJsonLd(sb, page, project);

        // Conditionally add FAQ JSON-LD if content has FAQ data
        AppendFaqJsonLdIfPresent(sb, page);

        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        // Header
        if (!string.IsNullOrWhiteSpace(project.HeaderHtml))
        {
            sb.AppendLine(project.HeaderHtml);
        }
        else
        {
            sb.AppendLine("  <header class=\"pseo-header\">");
            if (!string.IsNullOrWhiteSpace(project.BackLinkUrl))
            {
                sb.AppendLine($"    <a href=\"{Encode(project.BackLinkUrl)}\">{Encode(project.BackLinkText)}</a>");
            }
            sb.AppendLine("  </header>");
        }

        // Main content
        sb.AppendLine("  <main class=\"pseo-content\">");
        var contentHtml = RenderContentBySlug(page, schema);
        sb.AppendLine(contentHtml);
        sb.AppendLine("  </main>");

        // CTA
        if (!string.IsNullOrWhiteSpace(project.CtaHtml))
        {
            sb.AppendLine(project.CtaHtml);
        }

        // Footer
        if (!string.IsNullOrWhiteSpace(project.FooterHtml))
        {
            sb.AppendLine(project.FooterHtml);
        }
        else
        {
            sb.AppendLine("  <footer class=\"pseo-footer\">");
            if (!string.IsNullOrWhiteSpace(project.BackLinkUrl))
            {
                sb.AppendLine($"    <a href=\"{Encode(project.BackLinkUrl)}\">{Encode(project.BackLinkText)}</a>");
            }
            sb.AppendLine("  </footer>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return Task.FromResult(sb.ToString());
    }

    /// <inheritdoc />
    public Task<string> RenderContentAsync(PseoPage page, ContentSchema schema)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(schema);

        var html = RenderContentBySlug(page, schema);
        return Task.FromResult(html);
    }

    // ───────────────────────────────────────────────────────
    // Content renderers dispatched by schema.RendererSlug
    // ───────────────────────────────────────────────────────

    private string RenderContentBySlug(PseoPage page, ContentSchema schema)
    {
        JsonDocument? doc = null;
        try
        {
            doc = JsonDocument.Parse(page.ContentJson);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse content JSON for page {PageId}", page.Id);
            return "<p class=\"pseo-error\">Content could not be rendered.</p>";
        }

        using (doc)
        {
            var root = doc.RootElement;

            return schema.RendererSlug switch
            {
                "idea-list" => RenderIdeaList(page.Title, root),
                "checklist" => RenderChecklist(page.Title, root),
                "faq" => RenderFaq(page.Title, root),
                "comparison" => RenderComparison(page.Title, root),
                "guide" => RenderGuide(page.Title, root),
                "listicle" => RenderListicle(page.Title, root),
                "how-to" => RenderHowTo(page.Title, root),
                "review" => RenderReview(page.Title, root),
                _ => RenderGeneric(page.Title, root)
            };
        }
    }

    /// <summary>
    /// Renders an idea list — a collection of titled ideas with descriptions.
    /// </summary>
    private static string RenderIdeaList(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("ideas", out var ideas) && ideas.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <div class=\"pseo-idea-list\">");
            var index = 1;
            foreach (var idea in ideas.EnumerateArray())
            {
                var ideaTitle = idea.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var ideaDesc = idea.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

                sb.AppendLine("      <article class=\"pseo-idea\">");
                sb.AppendLine($"        <h2>{index}. {Encode(ideaTitle)}</h2>");
                sb.AppendLine($"        <p>{Encode(ideaDesc)}</p>");
                sb.AppendLine("      </article>");
                index++;
            }
            sb.AppendLine("    </div>");
        }

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders a checklist — actionable items with optional tips.
    /// </summary>
    private static string RenderChecklist(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <ul class=\"pseo-checklist\">");
            foreach (var item in items.EnumerateArray())
            {
                var text = item.TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
                var tip = item.TryGetProperty("tip", out var tp) ? tp.GetString() : null;

                sb.AppendLine("      <li class=\"pseo-checklist-item\">");
                sb.AppendLine($"        <span class=\"pseo-check-text\">{Encode(text)}</span>");
                if (!string.IsNullOrWhiteSpace(tip))
                    sb.AppendLine($"        <span class=\"pseo-check-tip\">{Encode(tip)}</span>");
                sb.AppendLine("      </li>");
            }
            sb.AppendLine("    </ul>");
        }

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders an FAQ — question/answer pairs with semantic markup.
    /// </summary>
    private static string RenderFaq(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("questions", out var questions) && questions.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <div class=\"pseo-faq-list\">");
            foreach (var q in questions.EnumerateArray())
            {
                var question = q.TryGetProperty("question", out var qe) ? qe.GetString() ?? "" : "";
                var answer = q.TryGetProperty("answer", out var a) ? a.GetString() ?? "" : "";

                sb.AppendLine("      <details class=\"pseo-faq-item\">");
                sb.AppendLine($"        <summary class=\"pseo-faq-question\">{Encode(question)}</summary>");
                sb.AppendLine($"        <div class=\"pseo-faq-answer\"><p>{Encode(answer)}</p></div>");
                sb.AppendLine("      </details>");
            }
            sb.AppendLine("    </div>");
        }

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders a comparison — side-by-side analysis of options.
    /// </summary>
    private static string RenderComparison(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("options", out var options) && options.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <div class=\"pseo-comparison-grid\">");
            foreach (var option in options.EnumerateArray())
            {
                var name = option.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                var description = option.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

                sb.AppendLine("      <div class=\"pseo-comparison-card\">");
                sb.AppendLine($"        <h2>{Encode(name)}</h2>");
                sb.AppendLine($"        <p>{Encode(description)}</p>");

                if (option.TryGetProperty("pros", out var pros) && pros.ValueKind == JsonValueKind.Array)
                {
                    sb.AppendLine("        <div class=\"pseo-pros\">");
                    sb.AppendLine("          <h3>Pros</h3>");
                    sb.AppendLine("          <ul>");
                    foreach (var pro in pros.EnumerateArray())
                        sb.AppendLine($"            <li>{Encode(pro.GetString() ?? "")}</li>");
                    sb.AppendLine("          </ul>");
                    sb.AppendLine("        </div>");
                }

                if (option.TryGetProperty("cons", out var cons) && cons.ValueKind == JsonValueKind.Array)
                {
                    sb.AppendLine("        <div class=\"pseo-cons\">");
                    sb.AppendLine("          <h3>Cons</h3>");
                    sb.AppendLine("          <ul>");
                    foreach (var con in cons.EnumerateArray())
                        sb.AppendLine($"            <li>{Encode(con.GetString() ?? "")}</li>");
                    sb.AppendLine("          </ul>");
                    sb.AppendLine("        </div>");
                }

                sb.AppendLine("      </div>");
            }
            sb.AppendLine("    </div>");
        }

        if (root.TryGetProperty("verdict", out var verdict))
            sb.AppendLine($"    <div class=\"pseo-verdict\"><h2>Verdict</h2><p>{Encode(verdict.GetString() ?? "")}</p></div>");

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders a guide — structured sections with headings and paragraphs.
    /// </summary>
    private static string RenderGuide(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
        {
            foreach (var section in sections.EnumerateArray())
            {
                var heading = section.TryGetProperty("heading", out var h) ? h.GetString() ?? "" : "";
                var content = section.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";

                sb.AppendLine("      <section class=\"pseo-guide-section\">");
                sb.AppendLine($"        <h2>{Encode(heading)}</h2>");
                sb.AppendLine($"        <p>{Encode(content)}</p>");
                sb.AppendLine("      </section>");
            }
        }

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders a listicle — numbered list items with descriptions.
    /// </summary>
    private static string RenderListicle(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("items", out var items) && items.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <ol class=\"pseo-listicle\">");
            foreach (var item in items.EnumerateArray())
            {
                var itemTitle = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var itemDesc = item.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";

                sb.AppendLine("      <li class=\"pseo-listicle-item\">");
                sb.AppendLine($"        <h2>{Encode(itemTitle)}</h2>");
                sb.AppendLine($"        <p>{Encode(itemDesc)}</p>");
                sb.AppendLine("      </li>");
            }
            sb.AppendLine("    </ol>");
        }

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders a how-to — step-by-step instructions.
    /// </summary>
    private static string RenderHowTo(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("steps", out var steps) && steps.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <ol class=\"pseo-how-to\">");
            var stepNum = 1;
            foreach (var step in steps.EnumerateArray())
            {
                var stepTitle = step.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                var instruction = step.TryGetProperty("instruction", out var i) ? i.GetString() ?? "" : "";
                var tip = step.TryGetProperty("tip", out var tp) ? tp.GetString() : null;

                sb.AppendLine("      <li class=\"pseo-step\">");
                sb.AppendLine($"        <h2>Step {stepNum}: {Encode(stepTitle)}</h2>");
                sb.AppendLine($"        <p>{Encode(instruction)}</p>");
                if (!string.IsNullOrWhiteSpace(tip))
                    sb.AppendLine($"        <p class=\"pseo-step-tip\"><strong>Tip:</strong> {Encode(tip)}</p>");
                sb.AppendLine("      </li>");
                stepNum++;
            }
            sb.AppendLine("    </ol>");
        }

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Renders a review — product or service review with rating and verdict.
    /// </summary>
    private static string RenderReview(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        if (root.TryGetProperty("introduction", out var intro))
            sb.AppendLine($"    <p class=\"pseo-intro\">{Encode(intro.GetString() ?? "")}</p>");

        if (root.TryGetProperty("rating", out var rating) && rating.ValueKind == JsonValueKind.Number)
        {
            sb.AppendLine($"    <div class=\"pseo-rating\">Rating: <strong>{rating.GetDecimal():F1}</strong>/5</div>");
        }

        if (root.TryGetProperty("sections", out var sections) && sections.ValueKind == JsonValueKind.Array)
        {
            foreach (var section in sections.EnumerateArray())
            {
                var heading = section.TryGetProperty("heading", out var h) ? h.GetString() ?? "" : "";
                var content = section.TryGetProperty("content", out var c) ? c.GetString() ?? "" : "";

                sb.AppendLine("      <section class=\"pseo-review-section\">");
                sb.AppendLine($"        <h2>{Encode(heading)}</h2>");
                sb.AppendLine($"        <p>{Encode(content)}</p>");
                sb.AppendLine("      </section>");
            }
        }

        if (root.TryGetProperty("pros", out var pros) && pros.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <div class=\"pseo-pros\">");
            sb.AppendLine("      <h3>Pros</h3>");
            sb.AppendLine("      <ul>");
            foreach (var pro in pros.EnumerateArray())
                sb.AppendLine($"        <li>{Encode(pro.GetString() ?? "")}</li>");
            sb.AppendLine("      </ul>");
            sb.AppendLine("    </div>");
        }

        if (root.TryGetProperty("cons", out var cons) && cons.ValueKind == JsonValueKind.Array)
        {
            sb.AppendLine("    <div class=\"pseo-cons\">");
            sb.AppendLine("      <h3>Cons</h3>");
            sb.AppendLine("      <ul>");
            foreach (var con in cons.EnumerateArray())
                sb.AppendLine($"        <li>{Encode(con.GetString() ?? "")}</li>");
            sb.AppendLine("      </ul>");
            sb.AppendLine("    </div>");
        }

        if (root.TryGetProperty("verdict", out var verdict))
            sb.AppendLine($"    <div class=\"pseo-verdict\"><h2>Verdict</h2><p>{Encode(verdict.GetString() ?? "")}</p></div>");

        AppendConclusion(sb, root);
        return sb.ToString();
    }

    /// <summary>
    /// Generic fallback renderer — iterates all JSON properties and produces semantic HTML.
    /// </summary>
    private static string RenderGeneric(string title, JsonElement root)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"    <h1 class=\"pseo-title\">{Encode(title)}</h1>");

        foreach (var prop in root.EnumerateObject())
        {
            switch (prop.Value.ValueKind)
            {
                case JsonValueKind.String:
                    var text = prop.Value.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var tag = prop.Name is "introduction" or "intro" or "conclusion" ? "p" : "p";
                        var cssClass = $"pseo-{prop.Name}";
                        sb.AppendLine($"    <div class=\"{cssClass}\"><{tag}>{Encode(text)}</{tag}></div>");
                    }
                    break;

                case JsonValueKind.Array:
                    sb.AppendLine($"    <div class=\"pseo-{prop.Name}\">");
                    sb.AppendLine($"      <h2>{Encode(FormatFieldName(prop.Name))}</h2>");
                    RenderGenericArray(sb, prop.Value);
                    sb.AppendLine("    </div>");
                    break;

                case JsonValueKind.Object:
                    sb.AppendLine($"    <div class=\"pseo-{prop.Name}\">");
                    sb.AppendLine($"      <h2>{Encode(FormatFieldName(prop.Name))}</h2>");
                    RenderGenericObject(sb, prop.Value);
                    sb.AppendLine("    </div>");
                    break;
            }
        }

        return sb.ToString();
    }

    private static void RenderGenericArray(StringBuilder sb, JsonElement array)
    {
        sb.AppendLine("      <ul>");
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                sb.AppendLine($"        <li>{Encode(item.GetString() ?? "")}</li>");
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                sb.AppendLine("        <li>");
                foreach (var prop in item.EnumerateObject())
                {
                    if (prop.Value.ValueKind == JsonValueKind.String)
                    {
                        var val = prop.Value.GetString() ?? "";
                        if (prop.Name is "title" or "name" or "heading" or "question")
                            sb.AppendLine($"          <strong>{Encode(val)}</strong>");
                        else
                            sb.AppendLine($"          <span>{Encode(val)}</span>");
                    }
                }
                sb.AppendLine("        </li>");
            }
        }
        sb.AppendLine("      </ul>");
    }

    private static void RenderGenericObject(StringBuilder sb, JsonElement obj)
    {
        foreach (var prop in obj.EnumerateObject())
        {
            if (prop.Value.ValueKind == JsonValueKind.String)
            {
                sb.AppendLine($"      <p><strong>{Encode(FormatFieldName(prop.Name))}:</strong> {Encode(prop.Value.GetString() ?? "")}</p>");
            }
        }
    }

    // ───────────────────────────────────────────────────────
    // JSON-LD structured data helpers
    // ───────────────────────────────────────────────────────

    /// <summary>
    /// Appends Article JSON-LD structured data to the head.
    /// </summary>
    private static void AppendArticleJsonLd(StringBuilder sb, PseoPage page, PseoProject project)
    {
        var canonicalUrl = $"https://{project.Fqdn}/{page.Slug}";

        var jsonLd = new
        {
            @context = "https://schema.org",
            @type = "Article",
            headline = page.Title,
            description = page.MetaDescription ?? "",
            url = canonicalUrl,
            datePublished = page.PublishedAt?.ToString("yyyy-MM-ddTHH:mm:ssZ") ?? page.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            dateModified = page.UpdatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            publisher = new
            {
                @type = "Organization",
                name = project.Name
            }
        };

        var json = JsonSerializer.Serialize(jsonLd, new JsonSerializerOptions { WriteIndented = false });
        sb.AppendLine($"  <script type=\"application/ld+json\">{json}</script>");
    }

    /// <summary>
    /// Appends BreadcrumbList JSON-LD structured data to the head.
    /// </summary>
    private static void AppendBreadcrumbJsonLd(StringBuilder sb, PseoPage page, PseoProject project)
    {
        var homeUrl = $"https://{project.Fqdn}/";
        var pageUrl = $"https://{project.Fqdn}/{page.Slug}";

        var jsonLd = new
        {
            @context = "https://schema.org",
            @type = "BreadcrumbList",
            itemListElement = new object[]
            {
                new { @type = "ListItem", position = 1, name = "Home", item = homeUrl },
                new { @type = "ListItem", position = 2, name = page.Title, item = pageUrl }
            }
        };

        var json = JsonSerializer.Serialize(jsonLd, new JsonSerializerOptions { WriteIndented = false });
        sb.AppendLine($"  <script type=\"application/ld+json\">{json}</script>");
    }

    /// <summary>
    /// Appends FAQPage JSON-LD if the content contains FAQ-style question/answer pairs.
    /// </summary>
    private static void AppendFaqJsonLdIfPresent(StringBuilder sb, PseoPage page)
    {
        try
        {
            using var doc = JsonDocument.Parse(page.ContentJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("questions", out var questions) || questions.ValueKind != JsonValueKind.Array)
                return;

            var faqEntries = new List<object>();
            foreach (var q in questions.EnumerateArray())
            {
                var question = q.TryGetProperty("question", out var qe) ? qe.GetString() ?? "" : "";
                var answer = q.TryGetProperty("answer", out var a) ? a.GetString() ?? "" : "";

                if (!string.IsNullOrWhiteSpace(question) && !string.IsNullOrWhiteSpace(answer))
                {
                    faqEntries.Add(new
                    {
                        @type = "Question",
                        name = question,
                        acceptedAnswer = new { @type = "Answer", text = answer }
                    });
                }
            }

            if (faqEntries.Count == 0)
                return;

            var jsonLd = new
            {
                @context = "https://schema.org",
                @type = "FAQPage",
                mainEntity = faqEntries
            };

            var json = JsonSerializer.Serialize(jsonLd, new JsonSerializerOptions { WriteIndented = false });
            sb.AppendLine($"  <script type=\"application/ld+json\">{json}</script>");
        }
        catch
        {
            // Content JSON parse failure is not fatal for JSON-LD
        }
    }

    // ───────────────────────────────────────────────────────
    // Utility helpers
    // ───────────────────────────────────────────────────────

    private static void AppendConclusion(StringBuilder sb, JsonElement root)
    {
        if (root.TryGetProperty("conclusion", out var conclusion))
        {
            var text = conclusion.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                sb.AppendLine($"    <div class=\"pseo-conclusion\"><p>{Encode(text)}</p></div>");
        }
    }

    /// <summary>
    /// HTML-encodes a string for safe inclusion in HTML output.
    /// </summary>
    private static string Encode(string value)
    {
        return WebUtility.HtmlEncode(value);
    }

    /// <summary>
    /// Converts a camelCase or snake_case field name into a human-readable title.
    /// </summary>
    private static string FormatFieldName(string fieldName)
    {
        // Convert camelCase to spaces
        var sb = new StringBuilder();
        for (var i = 0; i < fieldName.Length; i++)
        {
            var c = fieldName[i];
            if (c == '_' || c == '-')
            {
                sb.Append(' ');
            }
            else if (i > 0 && char.IsUpper(c) && !char.IsUpper(fieldName[i - 1]))
            {
                sb.Append(' ');
                sb.Append(c);
            }
            else
            {
                sb.Append(i == 0 ? char.ToUpper(c) : c);
            }
        }
        return sb.ToString();
    }
}
