using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing pSEO projects — standalone sites with custom FQDN, chrome, and content generation.
/// </summary>
public interface IPseoProjectService
{
    Task<PseoProject?> GetByIdAsync(Guid id);
    Task<PseoProject?> GetByFqdnAsync(string fqdn);
    Task<List<PseoProject>> GetBySiteIdAsync(Guid siteId);
    Task<PseoProject> CreateAsync(PseoProject project);
    Task<PseoProject> UpdateAsync(PseoProject project);
    Task UpdateChromeAsync(Guid id, string? headerHtml, string? footerHtml, string? customCss);
    Task UpdateStatusAsync(Guid id, string status);
    Task DeleteAsync(Guid id);
    Task<bool> CheckDnsAsync(string fqdn);
    Task<DnsVerificationResult> VerifyDnsAsync(Guid projectId);
    Task<List<PseoProject>> GetPendingDnsProjectsAsync();
}
