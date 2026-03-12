namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing the publishing pipeline — batch publishing, pausing, and resuming pSEO pages.
/// </summary>
public interface IPublishService
{
    /// <summary>
    /// Publishes the next batch of generated pages within a collection.
    /// </summary>
    /// <param name="collectionId">The collection to publish from.</param>
    /// <param name="batchSize">Maximum number of pages to publish in this batch.</param>
    Task PublishBatchAsync(Guid collectionId, int batchSize);

    /// <summary>
    /// Publishes a single page immediately.
    /// </summary>
    /// <param name="pageId">The page to publish.</param>
    Task PublishPageAsync(Guid pageId);

    /// <summary>
    /// Unpublishes a single page, reverting it to generated status.
    /// </summary>
    /// <param name="pageId">The page to unpublish.</param>
    Task UnpublishPageAsync(Guid pageId);

    /// <summary>
    /// Pauses the publishing pipeline for a collection.
    /// </summary>
    /// <param name="collectionId">The collection to pause.</param>
    Task PausePublishingAsync(Guid collectionId);

    /// <summary>
    /// Resumes the publishing pipeline for a collection.
    /// </summary>
    /// <param name="collectionId">The collection to resume.</param>
    Task ResumePublishingAsync(Guid collectionId);
}
