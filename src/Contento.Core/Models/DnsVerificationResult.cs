namespace Contento.Core.Models;

/// <summary>
/// Result of a DNS/CNAME verification check for a pSEO project.
/// </summary>
public class DnsVerificationResult
{
    /// <summary>
    /// Whether the DNS is verified and the CNAME is resolving.
    /// </summary>
    public bool IsVerified { get; set; }

    /// <summary>
    /// Current status: pending_dns, active, dns_failed.
    /// </summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// The actual CNAME target that was resolved (if any).
    /// </summary>
    public string? CnameTarget { get; set; }

    /// <summary>
    /// The expected CNAME target (e.g., pseo.contentocms.com).
    /// </summary>
    public string? ExpectedTarget { get; set; }

    /// <summary>
    /// Human-readable message describing the verification result.
    /// </summary>
    public string? Message { get; set; }
}
