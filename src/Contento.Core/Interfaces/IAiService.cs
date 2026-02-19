using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// AI writing assistant and content generation service (BYOK via AIGW)
/// </summary>
public interface IAiService
{
    Task<AiCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, AiSettings settings);
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string userPrompt, AiSettings settings, CancellationToken ct = default);
    Task<Theme?> GenerateThemeAsync(string prompt, AiSettings settings);
    Task<Layout?> GenerateLayoutAsync(string prompt, Guid siteId, AiSettings settings);
}

/// <summary>
/// Result of a non-streaming AI completion
/// </summary>
public class AiCompletionResult
{
    public bool Success { get; set; }
    public string Text { get; set; } = "";
    public string? Error { get; set; }
    public int TokensUsed { get; set; }
}
