namespace Contento.Core.Interfaces;

/// <summary>
/// oEmbed consumer service for resolving URLs to rich embed HTML.
/// </summary>
public interface IOEmbedService
{
    Task<OEmbedResult?> ResolveAsync(string url);
    string ProcessContent(string html);
}

public class OEmbedResult
{
    public string Type { get; set; } = string.Empty;
    public string Html { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public int? Width { get; set; }
    public int? Height { get; set; }
}
