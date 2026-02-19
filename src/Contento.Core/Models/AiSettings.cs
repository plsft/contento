namespace Contento.Core.Models;

/// <summary>
/// BYOK AI settings stored in Site.Settings JSON under the "ai" key
/// </summary>
public class AiSettings
{
    public string Provider { get; set; } = "openai";
    public string Model { get; set; } = "openai/gpt-4o-mini";
    public string ApiKey { get; set; } = "";
    public bool Enabled { get; set; }
}
