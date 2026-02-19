using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Noundry.Guardian;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Services;

/// <summary>
/// oEmbed consumer that resolves URLs from known providers into rich embed HTML.
/// Results are cached in-memory with a 5-minute expiry.
/// </summary>
public class OEmbedService : IOEmbedService
{
    private readonly ILogger<OEmbedService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    private static readonly ConcurrentDictionary<string, (OEmbedResult Result, DateTime ExpiresAt)> Cache = new();
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(5);

    private static readonly Regex StandaloneUrlPattern = new(
        @"<p>\s*(https?://[^\s<]+)\s*</p>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Known oEmbed providers mapped as (regex pattern -> endpoint URL template).
    /// The endpoint template uses {0} as a placeholder for the URL-encoded source URL.
    /// </summary>
    public static readonly List<(Regex Pattern, string Endpoint)> Providers =
    [
        // YouTube
        (new Regex(@"https?://(?:www\.)?youtube\.com/watch\?v=[\w-]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://www.youtube.com/oembed?url={0}&format=json"),
        (new Regex(@"https?://youtu\.be/[\w-]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://www.youtube.com/oembed?url={0}&format=json"),

        // Vimeo
        (new Regex(@"https?://(?:www\.)?vimeo\.com/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://vimeo.com/api/oembed.json?url={0}"),

        // Twitter / X
        (new Regex(@"https?://(?:www\.)?(?:twitter\.com|x\.com)/\w+/status/\d+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://publish.twitter.com/oembed?url={0}"),

        // Spotify
        (new Regex(@"https?://open\.spotify\.com/(?:track|album|playlist|episode)/[\w]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://open.spotify.com/oembed?url={0}"),

        // SoundCloud
        (new Regex(@"https?://soundcloud\.com/[\w-]+/[\w-]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://soundcloud.com/oembed?url={0}&format=json"),

        // CodePen
        (new Regex(@"https?://codepen\.io/[\w-]+/pen/[\w]+", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            "https://codepen.io/api/oembed?url={0}&format=json"),
    ];

    /// <summary>
    /// Initializes a new instance of <see cref="OEmbedService"/>.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public OEmbedService(ILogger<OEmbedService> logger, IHttpClientFactory httpClientFactory)
    {
        _logger = Guard.Against.Null(logger);
        _httpClientFactory = Guard.Against.Null(httpClientFactory);
    }

    /// <inheritdoc />
    public async Task<OEmbedResult?> ResolveAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;

        // Check cache first
        if (Cache.TryGetValue(url, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
            return cached.Result;

        // Remove expired entry if present
        if (cached.ExpiresAt != default)
            Cache.TryRemove(url, out _);

        // Find matching provider
        string? endpointTemplate = null;
        foreach (var (pattern, endpoint) in Providers)
        {
            if (pattern.IsMatch(url))
            {
                endpointTemplate = endpoint;
                break;
            }
        }

        if (endpointTemplate == null)
            return null;

        try
        {
            var requestUrl = string.Format(endpointTemplate, Uri.EscapeDataString(url));
            var client = _httpClientFactory.CreateClient("oembed");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("oEmbed request failed for {Url}: {StatusCode}", url, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadFromJsonAsync<OEmbedJsonResponse>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (json == null)
                return null;

            var result = new OEmbedResult
            {
                Type = json.Type ?? "rich",
                Html = json.Html ?? string.Empty,
                Title = json.Title,
                ThumbnailUrl = json.ThumbnailUrl,
                ProviderName = json.ProviderName ?? string.Empty,
                Width = json.Width,
                Height = json.Height
            };

            Cache[url] = (result, DateTime.UtcNow.Add(CacheExpiry));

            _logger.LogInformation("Resolved oEmbed for {Url} from {Provider}", url, result.ProviderName);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve oEmbed for {Url}", url);
            return null;
        }
    }

    /// <inheritdoc />
    public string ProcessContent(string html)
    {
        if (string.IsNullOrEmpty(html))
            return string.Empty;

        return StandaloneUrlPattern.Replace(html, match =>
        {
            var url = match.Groups[1].Value;

            try
            {
                var result = ResolveAsync(url).GetAwaiter().GetResult();
                if (result != null && !string.IsNullOrEmpty(result.Html))
                {
                    return $"<div class=\"oembed-embed oembed-{result.Type}\">{result.Html}</div>";
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process oEmbed for inline URL {Url}", url);
            }

            // Leave unresolvable URLs unchanged
            return match.Value;
        });
    }

    /// <summary>
    /// Internal JSON model for deserializing oEmbed API responses.
    /// </summary>
    private class OEmbedJsonResponse
    {
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("html")]
        public string? Html { get; set; }

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("thumbnail_url")]
        public string? ThumbnailUrl { get; set; }

        [JsonPropertyName("provider_name")]
        public string? ProviderName { get; set; }

        [JsonPropertyName("width")]
        public int? Width { get; set; }

        [JsonPropertyName("height")]
        public int? Height { get; set; }
    }
}
