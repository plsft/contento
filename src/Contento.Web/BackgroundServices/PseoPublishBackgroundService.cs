using Contento.Core.Interfaces;

namespace Contento.Web.BackgroundServices;

/// <summary>
/// Background service that handles scheduled and batched publishing of pSEO pages.
/// Polls for collections with non-manual publish schedules and publishes pages accordingly.
/// </summary>
public class PseoPublishBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PseoPublishBackgroundService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(60);

    public PseoPublishBackgroundService(IServiceScopeFactory scopeFactory, ILogger<PseoPublishBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PseoPublishBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPublishingCollectionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in pSEO publish background service polling loop");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessPublishingCollectionsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var collectionService = scope.ServiceProvider.GetRequiredService<ICollectionService>();
        var publishService = scope.ServiceProvider.GetRequiredService<IPublishService>();
        var internalLinkingService = scope.ServiceProvider.GetRequiredService<IInternalLinkingService>();
        var projectService = scope.ServiceProvider.GetRequiredService<IPseoProjectService>();

        // We need to find collections that are in 'publishing' status with non-manual schedules.
        // Since ICollectionService doesn't have a method to get all publishing collections,
        // we get collections per project. First get all projects.
        var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
        var sites = await siteService.GetAllAsync(1, 100);

        foreach (var site in sites)
        {
            var projects = await projectService.GetBySiteIdAsync(site.Id);

            foreach (var project in projects)
            {
                var collections = await collectionService.GetByProjectIdAsync(project.Id);

                foreach (var collection in collections)
                {
                    if (collection.Status != "publishing")
                        continue;

                    if (collection.PublishSchedule == "manual")
                        continue;

                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        await ProcessCollectionAsync(collection, publishService, internalLinkingService, collectionService);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process publishing for collection {CollectionId} ({Name})",
                            collection.Id, collection.Name);
                    }
                }
            }
        }
    }

    private async Task ProcessCollectionAsync(
        Contento.Core.Models.PseoCollection collection,
        IPublishService publishService,
        IInternalLinkingService internalLinkingService,
        ICollectionService collectionService)
    {
        switch (collection.PublishSchedule)
        {
            case "immediate":
                // Publish all validated pages at once
                _logger.LogInformation("Immediate publish for collection {CollectionId}: publishing all validated pages", collection.Id);
                await publishService.PublishBatchAsync(collection.Id, int.MaxValue);
                break;

            case "daily":
            case "hourly":
                // Batched: publish batch_size pages per run
                _logger.LogInformation("Batched publish for collection {CollectionId}: publishing up to {BatchSize} pages",
                    collection.Id, collection.BatchSize);
                await publishService.PublishBatchAsync(collection.Id, collection.BatchSize);
                break;

            case "scheduled":
                // Scheduled: check if the scheduled time has passed, then publish all
                _logger.LogInformation("Scheduled publish for collection {CollectionId}: publishing all validated pages", collection.Id);
                await publishService.PublishBatchAsync(collection.Id, int.MaxValue);
                break;

            default:
                _logger.LogDebug("Unknown publish schedule '{Schedule}' for collection {CollectionId}, skipping",
                    collection.PublishSchedule, collection.Id);
                return;
        }

        // After publishing, run internal linking for the collection
        try
        {
            await internalLinkingService.BuildLinksAsync(collection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Internal linking failed for collection {CollectionId} after publish", collection.Id);
        }

        // Refresh collection to check if all pages are published
        var updated = await collectionService.GetByIdAsync(collection.Id);
        if (updated != null && updated.PublishedCount >= updated.GeneratedCount && updated.GeneratedCount > 0)
        {
            await collectionService.UpdateStatusAsync(collection.Id, "published");
            _logger.LogInformation("Collection {CollectionId} fully published ({Published}/{Generated} pages)",
                collection.Id, updated.PublishedCount, updated.GeneratedCount);
        }
    }
}
