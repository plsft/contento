using Microsoft.Extensions.Options;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Web.BackgroundServices;

/// <summary>
/// Background service that periodically checks DNS resolution for pSEO projects
/// in 'pending_dns' status. When a CNAME resolves, the project transitions to 'active'.
/// Projects that remain unresolved beyond the configured timeout are marked 'dns_failed'.
/// </summary>
public class DnsVerificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DnsVerificationBackgroundService> _logger;
    private readonly PseoOptions _options;

    public DnsVerificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<DnsVerificationBackgroundService> logger,
        IOptions<PseoOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromMinutes(_options.DnsCheckIntervalMinutes);
        _logger.LogInformation(
            "DnsVerificationBackgroundService started (interval: {IntervalMinutes}m, timeout: {TimeoutHours}h)",
            _options.DnsCheckIntervalMinutes, _options.DnsTimeoutHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckPendingProjectsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DNS verification polling loop");
            }

            await Task.Delay(pollInterval, stoppingToken);
        }
    }

    private async Task CheckPendingProjectsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var projectService = scope.ServiceProvider.GetRequiredService<IPseoProjectService>();

        var pendingProjects = await projectService.GetPendingDnsProjectsAsync();

        if (pendingProjects.Count == 0)
            return;

        _logger.LogDebug("Checking DNS for {Count} pending project(s)", pendingProjects.Count);

        foreach (var project in pendingProjects)
        {
            if (ct.IsCancellationRequested)
                break;

            // Check if the project has exceeded the DNS timeout window
            var timeoutThreshold = project.CreatedAt.AddHours(_options.DnsTimeoutHours);
            if (DateTime.UtcNow > timeoutThreshold)
            {
                _logger.LogWarning(
                    "pSEO project {ProjectId} ({Fqdn}) exceeded DNS timeout of {TimeoutHours}h. Marking as dns_failed.",
                    project.Id, project.Fqdn, _options.DnsTimeoutHours);

                await projectService.UpdateStatusAsync(project.Id, "dns_failed");
                continue;
            }

            try
            {
                var result = await projectService.VerifyDnsAsync(project.Id);

                if (result.IsVerified)
                {
                    _logger.LogInformation(
                        "DNS verified for pSEO project {ProjectId} ({Fqdn}) — status transitioned to active.",
                        project.Id, project.Fqdn);
                }
                else
                {
                    _logger.LogDebug(
                        "DNS not yet resolved for pSEO project {ProjectId} ({Fqdn}): {Message}",
                        project.Id, project.Fqdn, result.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Error verifying DNS for pSEO project {ProjectId} ({Fqdn})",
                    project.Id, project.Fqdn);
            }
        }
    }
}
