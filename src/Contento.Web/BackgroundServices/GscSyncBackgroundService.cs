using System.Text.Json.Nodes;
using Contento.Core.Interfaces;

namespace Contento.Web.BackgroundServices;

/// <summary>
/// Background service that syncs Google Search Console data daily for all pSEO projects with GSC tokens configured.
/// Checks every hour, syncs once per day per project.
/// </summary>
public class GscSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GscSyncBackgroundService> _logger;
    private static readonly TimeSpan PollInterval = TimeSpan.FromHours(1);

    public GscSyncBackgroundService(IServiceScopeFactory scopeFactory, ILogger<GscSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GscSyncBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllProjectsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GSC sync polling loop");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task SyncAllProjectsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var siteService = scope.ServiceProvider.GetRequiredService<ISiteService>();
        var projectService = scope.ServiceProvider.GetRequiredService<IPseoProjectService>();
        var analyticsService = scope.ServiceProvider.GetRequiredService<IPseoAnalyticsService>();

        // Get all sites, then all projects
        var sites = await siteService.GetAllAsync(1, 1000);

        foreach (var site in sites)
        {
            var projects = await projectService.GetBySiteIdAsync(site.Id);

            foreach (var project in projects)
            {
                if (project.Status != "active")
                    continue;

                try
                {
                    var settings = JsonNode.Parse(project.Settings ?? "{}") as JsonObject;
                    var accessToken = settings?["gsc_access_token"]?.GetValue<string>();

                    if (string.IsNullOrEmpty(accessToken))
                        continue; // No GSC tokens configured

                    // Check last sync time — only sync once per day
                    var lastSyncStr = settings?["gsc_last_sync"]?.GetValue<string>();
                    if (!string.IsNullOrEmpty(lastSyncStr) && DateTime.TryParse(lastSyncStr, out var lastSync))
                    {
                        if (lastSync.Date >= DateTime.UtcNow.Date)
                        {
                            _logger.LogDebug("GSC already synced today for project {ProjectId}, skipping", project.Id);
                            continue;
                        }
                    }

                    _logger.LogInformation("Starting GSC sync for project {ProjectId} ({ProjectName})", project.Id, project.Name);
                    await analyticsService.SyncGscDataAsync(project.Id, ct);
                    _logger.LogInformation("GSC sync completed for project {ProjectId}", project.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "GSC sync failed for project {ProjectId}", project.Id);
                }
            }
        }
    }
}
