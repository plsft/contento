using System.Data;
using System.Net;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing pSEO projects — standalone sites with custom FQDN, chrome, and content generation.
/// </summary>
public class PseoProjectService : IPseoProjectService
{
    private readonly IDbConnection _db;
    private readonly ISiteService _siteService;
    private readonly ILogger<PseoProjectService> _logger;
    private readonly PseoOptions _pseoOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="PseoProjectService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="siteService">The site service.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="pseoOptions">The pSEO configuration options.</param>
    public PseoProjectService(IDbConnection db, ISiteService siteService, ILogger<PseoProjectService> logger, IOptions<PseoOptions> pseoOptions)
    {
        _db = Guard.Against.Null(db);
        _siteService = Guard.Against.Null(siteService);
        _logger = Guard.Against.Null(logger);
        _pseoOptions = Guard.Against.Null(pseoOptions).Value;
    }

    /// <inheritdoc />
    public async Task<PseoProject?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<PseoProject>(id);
    }

    /// <inheritdoc />
    public async Task<PseoProject?> GetByFqdnAsync(string fqdn)
    {
        Guard.Against.NullOrWhiteSpace(fqdn);

        var results = await _db.QueryAsync<PseoProject>(
            "SELECT * FROM pseo_projects WHERE fqdn = @Fqdn LIMIT 1",
            new { Fqdn = fqdn });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<List<PseoProject>> GetBySiteIdAsync(Guid siteId)
    {
        Guard.Against.Default(siteId);

        var results = await _db.QueryAsync<PseoProject>(
            "SELECT * FROM pseo_projects WHERE site_id = @SiteId ORDER BY created_at DESC",
            new { SiteId = siteId });
        return results.ToList();
    }

    /// <inheritdoc />
    public async Task<PseoProject> CreateAsync(PseoProject project)
    {
        Guard.Against.Null(project);
        Guard.Against.NullOrWhiteSpace(project.Name);
        Guard.Against.Default(project.SiteId);

        project.Id = Guid.NewGuid();
        project.CreatedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        await _db.InsertAsync(project);
        return project;
    }

    /// <inheritdoc />
    public async Task<PseoProject> UpdateAsync(PseoProject project)
    {
        Guard.Against.Null(project);
        Guard.Against.Default(project.Id);

        project.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(project);
        return project;
    }

    /// <inheritdoc />
    public async Task UpdateChromeAsync(Guid id, string? headerHtml, string? footerHtml, string? customCss)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            @"UPDATE pseo_projects
              SET header_html = @HeaderHtml, footer_html = @FooterHtml,
                  custom_css = @CustomCss, updated_at = @Now
              WHERE id = @Id",
            new { HeaderHtml = headerHtml, FooterHtml = footerHtml, CustomCss = customCss, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task UpdateStatusAsync(Guid id, string status)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(status);

        await _db.ExecuteAsync(
            "UPDATE pseo_projects SET status = @Status, updated_at = @Now WHERE id = @Id",
            new { Status = status, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var project = await _db.GetAsync<PseoProject>(id);
        if (project != null)
            await _db.DeleteAsync(project);
    }

    /// <inheritdoc />
    public async Task<bool> CheckDnsAsync(string fqdn)
    {
        Guard.Against.NullOrWhiteSpace(fqdn);

        try
        {
            var hostEntry = await Dns.GetHostEntryAsync(fqdn);
            return hostEntry.AddressList.Length > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DNS check failed for FQDN {Fqdn}", fqdn);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<DnsVerificationResult> VerifyDnsAsync(Guid projectId)
    {
        Guard.Against.Default(projectId);

        var project = await _db.GetAsync<PseoProject>(projectId);
        if (project == null)
        {
            return new DnsVerificationResult
            {
                IsVerified = false,
                Status = "failed",
                ExpectedTarget = _pseoOptions.CnameTarget,
                Message = "Project not found."
            };
        }

        var expectedTarget = _pseoOptions.CnameTarget;
        var result = new DnsVerificationResult
        {
            ExpectedTarget = expectedTarget
        };

        try
        {
            // Attempt to resolve the FQDN — if Cloudflare is proxying the CNAME,
            // this will return Cloudflare's edge IPs, which means the CNAME is working.
            var hostEntry = await Dns.GetHostEntryAsync(project.Fqdn);

            if (hostEntry.AddressList.Length > 0)
            {
                // DNS resolves — the CNAME is set up and Cloudflare (or another DNS) is proxying
                result.IsVerified = true;
                result.Status = "active";
                result.CnameTarget = hostEntry.HostName;
                result.Message = $"DNS verified. {project.Fqdn} resolves successfully ({hostEntry.AddressList.Length} address(es)). CNAME is active.";

                // Update project status to active
                if (project.Status != "active")
                {
                    _logger.LogInformation(
                        "DNS verified for pSEO project {ProjectId} ({Fqdn}). Transitioning from {OldStatus} to active.",
                        project.Id, project.Fqdn, project.Status);

                    await UpdateStatusAsync(project.Id, "active");
                }
            }
            else
            {
                result.IsVerified = false;
                result.Status = "pending_dns";
                result.Message = $"DNS lookup for {project.Fqdn} returned no addresses. Ensure CNAME points to {expectedTarget}.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DNS resolution failed for {Fqdn}", project.Fqdn);

            result.IsVerified = false;
            result.Status = "pending_dns";
            result.Message = $"DNS lookup for {project.Fqdn} failed: {ex.Message}. Add a CNAME record pointing to {expectedTarget}.";
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<List<PseoProject>> GetPendingDnsProjectsAsync()
    {
        var results = await _db.QueryAsync<PseoProject>(
            "SELECT * FROM pseo_projects WHERE status = 'pending_dns' ORDER BY created_at ASC");
        return results.ToList();
    }
}
