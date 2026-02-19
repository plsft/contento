namespace Contento.Core.Interfaces;

/// <summary>
/// Shortcode parser and processor for expanding shortcode tags in content.
/// </summary>
public interface IShortcodeProcessor
{
    string Process(string content, ShortcodeContext? context = null);
    void Register(string name, Func<Dictionary<string, string>, string?, string> handler);
}

public class ShortcodeContext
{
    public Guid? PostId { get; set; }
    public Guid? SiteId { get; set; }
    public string? BaseUrl { get; set; }
}
