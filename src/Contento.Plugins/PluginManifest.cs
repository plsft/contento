using System.Text.Json;
using System.Text.Json.Serialization;

namespace Contento.Plugins;

/// <summary>
/// Represents a plugin's manifest file (plugin.json) that describes the plugin's
/// metadata, permissions, and hooks.
/// </summary>
public class PluginManifest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("author")]
    public string Author { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("entryPoint")]
    public string EntryPoint { get; set; } = "index.js";

    [JsonPropertyName("permissions")]
    public string[] Permissions { get; set; } = [];

    [JsonPropertyName("hooks")]
    public string[] Hooks { get; set; } = [];

    [JsonPropertyName("settings")]
    public JsonElement? Settings { get; set; }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name)
            && !string.IsNullOrWhiteSpace(Slug)
            && !string.IsNullOrWhiteSpace(Version)
            && !string.IsNullOrWhiteSpace(EntryPoint);
    }

    public static PluginManifest? FromJson(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<PluginManifest>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }
}
