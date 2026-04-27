namespace Contento.Core.Models;

/// <summary>
/// Configuration options for the pSEO engine.
/// </summary>
public class PseoOptions
{
    /// <summary>
    /// The CNAME target that custom domains must point to (e.g., pseo.contentocms.com).
    /// </summary>
    public string CnameTarget { get; set; } = "pseo.contentocms.com";

    /// <summary>
    /// Default back-link text shown on pSEO pages.
    /// </summary>
    public string DefaultBackLinkText { get; set; } = "Back to our site";

    /// <summary>
    /// Maximum number of pages allowed per pSEO project.
    /// </summary>
    public int MaxPagesPerProject { get; set; } = 50000;

    /// <summary>
    /// Maximum number of concurrent page generation tasks.
    /// </summary>
    public int MaxConcurrentGenerations { get; set; } = 10;

    /// <summary>
    /// How often (in minutes) the background service checks DNS for pending projects.
    /// </summary>
    public int DnsCheckIntervalMinutes { get; set; } = 5;

    /// <summary>
    /// How many hours to wait before marking a pending DNS project as failed.
    /// </summary>
    public int DnsTimeoutHours { get; set; } = 48;
}
