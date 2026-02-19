using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Noundry.AIG.Client;
using Noundry.AIG.Client.Builders;
using Noundry.AIG.Core.Extensions;
using Noundry.AIG.Core.Models;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

public class AiService : IAiService
{
    private readonly IAigClient _aigClient;
    private readonly ILogger<AiService> _logger;

    private static readonly Lazy<string> WritingSystemPrompt = new(() => LoadDoc("writing-assistant.md"));
    private static readonly Lazy<string> ThemeSystemPrompt = new(() => LoadDoc("theme-generation.md"));
    private static readonly Lazy<string> LayoutSystemPrompt = new(() => LoadDoc("layout-generation.md"));

    public AiService(IAigClient aigClient, ILogger<AiService> logger)
    {
        _aigClient = aigClient ?? throw new ArgumentNullException(nameof(aigClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AiCompletionResult> CompleteAsync(string systemPrompt, string userPrompt, AiSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return new AiCompletionResult { Success = false, Error = "API key is not configured." };

        try
        {
            var request = new PromptBuilder()
                .WithModel(settings.Model)
                .WithMaxTokens(4096)
                .WithTemperature(0.7f)
                .AddSystemMessage(systemPrompt)
                .AddUserMessage(userPrompt)
                .Build();

            var providerConfig = new ProviderConfig { ApiKey = settings.ApiKey };
            var response = await _aigClient.SendAsync(request, providerConfig);

            if (response.IsSuccess())
            {
                return new AiCompletionResult
                {
                    Success = true,
                    Text = response.GetTextContent(),
                    TokensUsed = response.GetTotalTokens()
                };
            }

            var errorMsg = response.Error?.Message ?? "Unknown error from AI provider.";
            _logger.LogWarning("AI completion failed: {Error}", errorMsg);
            return new AiCompletionResult { Success = false, Error = errorMsg };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI completion threw an exception");
            return new AiCompletionResult { Success = false, Error = ex.Message };
        }
    }

    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt, string userPrompt, AiSettings settings,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            yield return "[Error: API key is not configured]";
            yield break;
        }

        var request = new PromptBuilder()
            .WithModel(settings.Model)
            .WithMaxTokens(4096)
            .WithTemperature(0.7f)
            .WithStreaming(true)
            .AddSystemMessage(systemPrompt)
            .AddUserMessage(userPrompt)
            .Build();

        var providerConfig = new ProviderConfig { ApiKey = settings.ApiKey };

        await foreach (var chunk in _aigClient.SendStreamAsync(request, providerConfig, ct))
        {
            var text = ExtractStreamChunk(chunk);
            if (!string.IsNullOrEmpty(text))
                yield return text;
        }
    }

    public async Task<Theme?> GenerateThemeAsync(string prompt, AiSettings settings)
    {
        var result = await CompleteAsync(ThemeSystemPrompt.Value, prompt, settings);
        if (!result.Success) return null;

        try
        {
            var json = ExtractJson(result.Text);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new Theme
            {
                Name = root.GetProperty("name").GetString() ?? "AI Theme",
                Slug = root.GetProperty("slug").GetString() ?? "ai-theme",
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Version = root.TryGetProperty("version", out var ver) ? ver.GetString() : "1.0.0",
                Author = root.TryGetProperty("author", out var auth) ? auth.GetString() : "AI Generated",
                CssVariables = root.GetProperty("cssVariables").GetRawText()
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI-generated theme JSON");
            return null;
        }
    }

    public async Task<Layout?> GenerateLayoutAsync(string prompt, Guid siteId, AiSettings settings)
    {
        var result = await CompleteAsync(LayoutSystemPrompt.Value, prompt, settings);
        if (!result.Success) return null;

        try
        {
            var json = ExtractJson(result.Text);
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new Layout
            {
                SiteId = siteId,
                Name = root.GetProperty("name").GetString() ?? "AI Layout",
                Slug = root.GetProperty("slug").GetString() ?? "ai-layout",
                Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Structure = root.GetProperty("structure").GetRawText(),
                CustomCss = root.TryGetProperty("customCss", out var css) ? css.GetString() : null,
                HeadContent = root.TryGetProperty("headContent", out var head) ? head.GetString() : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI-generated layout JSON");
            return null;
        }
    }

    private static string ExtractStreamChunk(AiResponse chunk)
    {
        // OpenAI streaming: choices[0].delta.content
        if (chunk.Choices?.Count > 0)
        {
            var delta = chunk.Choices[0].Delta;
            if (delta?.Content != null)
                return delta.Content;
        }

        // Anthropic streaming: content[0].text
        if (chunk.Content?.Count > 0)
        {
            var text = chunk.Content[0].Text;
            if (!string.IsNullOrEmpty(text))
                return text;
        }

        return string.Empty;
    }

    /// <summary>
    /// Extract JSON object from a response that may contain markdown fences or preamble
    /// </summary>
    private static string ExtractJson(string text)
    {
        // Try to find JSON in markdown code fence
        var fenceMatch = Regex.Match(text, @"```(?:json)?\s*\n?([\s\S]*?)\n?```", RegexOptions.Singleline);
        if (fenceMatch.Success)
            return fenceMatch.Groups[1].Value.Trim();

        // Try to find a top-level JSON object
        var braceStart = text.IndexOf('{');
        var braceEnd = text.LastIndexOf('}');
        if (braceStart >= 0 && braceEnd > braceStart)
            return text[braceStart..(braceEnd + 1)];

        return text.Trim();
    }

    private static string LoadDoc(string filename)
    {
        // Try to load from the Docs/Ai directory relative to the web root
        var paths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Docs", "Ai", filename),
            Path.Combine(Directory.GetCurrentDirectory(), "Docs", "Ai", filename),
        };

        foreach (var path in paths)
        {
            if (File.Exists(path))
                return File.ReadAllText(path);
        }

        // Fallback: return minimal instruction
        return $"You are an AI assistant. Follow the user's instructions precisely. Return only the requested content.";
    }
}
