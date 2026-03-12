using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Builds internal cross-links between pSEO pages within a collection,
/// injecting a "Related Articles" section into page HTML.
/// </summary>
public class InternalLinkingService : IInternalLinkingService
{
    private readonly IPseoPageService _pageService;
    private readonly ICollectionService _collectionService;
    private readonly ILogger<InternalLinkingService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="InternalLinkingService"/>.
    /// </summary>
    public InternalLinkingService(
        IPseoPageService pageService,
        ICollectionService collectionService,
        ILogger<InternalLinkingService> logger)
    {
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task BuildLinksAsync(Guid collectionId, int linksPerPage = 3)
    {
        var publishedPages = await _pageService.GetByCollectionIdAsync(collectionId, "published", 1, int.MaxValue);
        if (publishedPages.Count < 2)
        {
            _logger.LogDebug("Collection {CollectionId} has fewer than 2 published pages, skipping internal linking", collectionId);
            return;
        }

        _logger.LogInformation("Building internal links for {Count} published pages in collection {CollectionId}",
            publishedPages.Count, collectionId);

        var linked = 0;

        foreach (var page in publishedPages)
        {
            try
            {
                var relatedPages = FindRelatedPages(page, publishedPages, linksPerPage);
                if (relatedPages.Count == 0)
                    continue;

                var updatedHtml = InjectRelatedSection(page.BodyHtml ?? "", relatedPages);
                if (updatedHtml != page.BodyHtml)
                {
                    page.BodyHtml = updatedHtml;
                    page.UpdatedAt = DateTime.UtcNow;
                    await _pageService.UpdateAsync(page);
                    linked++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build internal links for page {PageId}", page.Id);
            }
        }

        _logger.LogInformation("Internal linking complete for collection {CollectionId}: {Linked} pages updated",
            collectionId, linked);
    }

    /// <inheritdoc />
    public async Task BuildLinksForPageAsync(Guid pageId, int maxLinks = 3)
    {
        var page = await _pageService.GetByIdAsync(pageId);
        if (page == null)
        {
            _logger.LogWarning("Page {PageId} not found for internal linking", pageId);
            return;
        }

        if (string.IsNullOrWhiteSpace(page.BodyHtml))
        {
            _logger.LogDebug("Page {PageId} has no BodyHtml, skipping internal linking", pageId);
            return;
        }

        // Get all published pages in the same collection
        var collectionPages = await _pageService.GetByCollectionIdAsync(page.CollectionId, "published", 1, int.MaxValue);
        if (collectionPages.Count < 2)
            return;

        var relatedPages = FindRelatedPages(page, collectionPages, maxLinks);
        if (relatedPages.Count == 0)
            return;

        var updatedHtml = InjectRelatedSection(page.BodyHtml, relatedPages);
        if (updatedHtml != page.BodyHtml)
        {
            page.BodyHtml = updatedHtml;
            page.UpdatedAt = DateTime.UtcNow;
            await _pageService.UpdateAsync(page);

            _logger.LogDebug("Injected {Count} internal links into page {PageId}", relatedPages.Count, pageId);
        }
    }

    /// <summary>
    /// Finds related pages for internal linking. Prefers same niche with different subtopic.
    /// Falls back to same collection with different niche.
    /// </summary>
    private static List<PseoPage> FindRelatedPages(PseoPage currentPage, List<PseoPage> allPages, int maxLinks)
    {
        var candidates = allPages
            .Where(p => p.Id != currentPage.Id)
            .ToList();

        // Priority 1: Same niche, different subtopic
        var sameNiche = candidates
            .Where(p => p.NicheSlug == currentPage.NicheSlug && p.Subtopic != currentPage.Subtopic)
            .ToList();

        // Priority 2: Different niche (adjacent niches)
        var differentNiche = candidates
            .Where(p => p.NicheSlug != currentPage.NicheSlug)
            .ToList();

        var result = new List<PseoPage>();

        // Take from same niche first
        result.AddRange(sameNiche.Take(maxLinks));

        // Fill remaining slots from different niches
        if (result.Count < maxLinks)
        {
            var remaining = maxLinks - result.Count;
            var existingIds = result.Select(r => r.Id).ToHashSet();
            result.AddRange(differentNiche
                .Where(p => !existingIds.Contains(p.Id))
                .Take(remaining));
        }

        return result;
    }

    /// <summary>
    /// Injects a "Related Articles" section into page HTML.
    /// Replaces any existing pseo-related section, or inserts before closing main tag.
    /// </summary>
    private static string InjectRelatedSection(string bodyHtml, List<PseoPage> relatedPages)
    {
        if (relatedPages.Count == 0)
            return bodyHtml;

        var links = string.Join("\n",
            relatedPages.Select(p =>
                $"      <li><a href=\"/{System.Net.WebUtility.HtmlEncode(p.Slug)}\">{System.Net.WebUtility.HtmlEncode(p.Title)}</a></li>"));

        var relatedSection = $"""
    <section class="pseo-related">
      <h2>Related Articles</h2>
      <ul>
{links}
      </ul>
    </section>
""";

        // Remove any existing related section first
        var cleaned = Regex.Replace(bodyHtml,
            @"<section class=""pseo-related"">[\s\S]*?</section>",
            "",
            RegexOptions.Singleline);

        // Try to inject before </main>
        var mainCloseIndex = cleaned.LastIndexOf("</main>", StringComparison.OrdinalIgnoreCase);
        if (mainCloseIndex >= 0)
        {
            return cleaned.Insert(mainCloseIndex, relatedSection + "\n  ");
        }

        // Fallback: append at the end
        return cleaned + "\n" + relatedSection;
    }
}
