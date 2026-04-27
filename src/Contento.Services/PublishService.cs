using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Manages the publishing pipeline — batch publishing, pausing, and resuming pSEO pages.
/// </summary>
public class PublishService : IPublishService
{
    private readonly IPseoPageService _pageService;
    private readonly ICollectionService _collectionService;
    private readonly IPseoRendererService _rendererService;
    private readonly IContentSchemaService _schemaService;
    private readonly ILogger<PublishService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="PublishService"/>.
    /// </summary>
    /// <param name="pageService">The pSEO page service.</param>
    /// <param name="collectionService">The collection service.</param>
    /// <param name="rendererService">The pSEO renderer service.</param>
    /// <param name="schemaService">The content schema service.</param>
    /// <param name="logger">The logger.</param>
    public PublishService(
        IPseoPageService pageService,
        ICollectionService collectionService,
        IPseoRendererService rendererService,
        IContentSchemaService schemaService,
        ILogger<PublishService> logger)
    {
        _pageService = pageService ?? throw new ArgumentNullException(nameof(pageService));
        _collectionService = collectionService ?? throw new ArgumentNullException(nameof(collectionService));
        _rendererService = rendererService ?? throw new ArgumentNullException(nameof(rendererService));
        _schemaService = schemaService ?? throw new ArgumentNullException(nameof(schemaService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task PublishBatchAsync(Guid collectionId, int batchSize)
    {
        var collection = await _collectionService.GetByIdAsync(collectionId)
            ?? throw new InvalidOperationException($"Collection {collectionId} not found");

        if (collection.Status == "paused")
        {
            _logger.LogInformation("Collection {CollectionId} is paused, skipping batch publish", collectionId);
            return;
        }

        var schema = await _schemaService.GetByIdAsync(collection.SchemaId)
            ?? throw new InvalidOperationException($"Content schema {collection.SchemaId} not found");

        var pendingPages = await _pageService.GetPendingPublishAsync(collectionId, batchSize);
        if (pendingPages.Count == 0)
        {
            _logger.LogDebug("No pending pages to publish for collection {CollectionId}", collectionId);
            return;
        }

        _logger.LogInformation("Publishing batch of {Count} pages for collection {CollectionId}",
            pendingPages.Count, collectionId);

        var publishedCount = 0;

        foreach (var page in pendingPages)
        {
            try
            {
                await PublishSinglePage(page, schema);
                publishedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish page {PageId}", page.Id);
            }
        }

        // Update collection published count
        await _collectionService.UpdateCountsAsync(
            collectionId,
            collection.GeneratedCount,
            collection.PublishedCount + publishedCount,
            collection.FailedCount);

        _logger.LogInformation("Published {Published} of {Total} pages in batch for collection {CollectionId}",
            publishedCount, pendingPages.Count, collectionId);
    }

    /// <inheritdoc />
    public async Task PublishPageAsync(Guid pageId)
    {
        var page = await _pageService.GetByIdAsync(pageId)
            ?? throw new InvalidOperationException($"Page {pageId} not found");

        var collection = await _collectionService.GetByIdAsync(page.CollectionId)
            ?? throw new InvalidOperationException($"Collection {page.CollectionId} not found");

        var schema = await _schemaService.GetByIdAsync(collection.SchemaId)
            ?? throw new InvalidOperationException($"Content schema {collection.SchemaId} not found");

        await PublishSinglePage(page, schema);

        // Update collection published count
        await _collectionService.UpdateCountsAsync(
            collection.Id,
            collection.GeneratedCount,
            collection.PublishedCount + 1,
            collection.FailedCount);

        _logger.LogInformation("Published page {PageId} ({Title})", pageId, page.Title);
    }

    /// <inheritdoc />
    public async Task UnpublishPageAsync(Guid pageId)
    {
        var page = await _pageService.GetByIdAsync(pageId)
            ?? throw new InvalidOperationException($"Page {pageId} not found");

        page.Status = "validated";
        page.PublishedAt = null;
        page.BodyHtml = null;
        page.UpdatedAt = DateTime.UtcNow;
        await _pageService.UpdateAsync(page);

        // Update collection published count
        var collection = await _collectionService.GetByIdAsync(page.CollectionId);
        if (collection != null)
        {
            var newPublishedCount = Math.Max(0, collection.PublishedCount - 1);
            await _collectionService.UpdateCountsAsync(
                collection.Id,
                collection.GeneratedCount,
                newPublishedCount,
                collection.FailedCount);
        }

        _logger.LogInformation("Unpublished page {PageId} ({Title})", pageId, page.Title);
    }

    /// <inheritdoc />
    public async Task PausePublishingAsync(Guid collectionId)
    {
        await _collectionService.UpdateStatusAsync(collectionId, "paused");
        _logger.LogInformation("Paused publishing for collection {CollectionId}", collectionId);
    }

    /// <inheritdoc />
    public async Task ResumePublishingAsync(Guid collectionId)
    {
        await _collectionService.UpdateStatusAsync(collectionId, "publishing");
        _logger.LogInformation("Resumed publishing for collection {CollectionId}", collectionId);
    }

    /// <summary>
    /// Renders and publishes a single page.
    /// </summary>
    private async Task PublishSinglePage(PseoPage page, ContentSchema schema)
    {
        var contentHtml = await _rendererService.RenderContentAsync(page, schema);

        page.BodyHtml = contentHtml;
        page.Status = "published";
        page.PublishedAt = DateTime.UtcNow;
        page.UpdatedAt = DateTime.UtcNow;
        await _pageService.UpdateAsync(page);
    }
}
