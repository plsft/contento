namespace Contento.Core.Interfaces;

public interface IPluginRuntime
{
    bool LoadPlugin(string pluginSlug, string entryPointCode, string? settings = null);
    string? InvokeHook(string pluginSlug, string hookName, string? contextJson = null);
    Dictionary<string, string?> BroadcastHook(string hookName, string? contextJson = null);
    bool UnloadPlugin(string pluginSlug);
    IReadOnlyList<string> GetLoadedPlugins();
    Task InitializeAsync(Guid siteId);
}
