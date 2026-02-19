using Jint;
using Jint.Native;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Plugins;

/// <summary>
/// Sandboxed JavaScript plugin host using Jint. Each plugin runs in an isolated
/// engine with restricted permissions, memory limits, and controlled API access.
/// Implements IPluginRuntime for DI registration.
/// </summary>
public class PluginHost : IPluginRuntime
{
    private readonly ILogger<PluginHost> _logger;
    private readonly IPluginService? _pluginService;
    private readonly Dictionary<string, Engine> _engines = new();

    private const int MaxMemoryMb = 64;
    private const int MaxExecutionMs = 5000;
    private const int MaxStatements = 100_000;

    public PluginHost(ILogger<PluginHost> logger, IPluginService? pluginService = null)
    {
        _logger = logger;
        _pluginService = pluginService;
    }

    /// <summary>
    /// Loads and initializes a plugin from its JavaScript entry point.
    /// Injects a `contento` event bus object for hook registration.
    /// </summary>
    public bool LoadPlugin(string pluginSlug, string entryPointCode, string? settings = null)
    {
        try
        {
            var engine = CreateSandboxedEngine();

            engine.SetValue("console", new
            {
                log = new Action<object>(msg => _logger.LogInformation("[Plugin:{Slug}] {Message}", pluginSlug, msg)),
                warn = new Action<object>(msg => _logger.LogWarning("[Plugin:{Slug}] {Message}", pluginSlug, msg)),
                error = new Action<object>(msg => _logger.LogError("[Plugin:{Slug}] {Message}", pluginSlug, msg))
            });

            // Inject contento event bus for hook registration
            engine.Execute("""
                var contento = {
                    _hooks: {},
                    on: function(hookName, callback) {
                        if (!this._hooks[hookName]) this._hooks[hookName] = [];
                        this._hooks[hookName].push(callback);
                    }
                };
                """);

            if (!string.IsNullOrWhiteSpace(settings))
            {
                engine.SetValue("__settings__", settings);
                engine.Execute("const PLUGIN_SETTINGS = JSON.parse(__settings__);");
            }
            else
            {
                engine.Execute("const PLUGIN_SETTINGS = {};");
            }

            engine.Execute(entryPointCode);

            _engines[pluginSlug] = engine;
            _logger.LogInformation("Plugin loaded: {Slug}", pluginSlug);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load plugin: {Slug}", pluginSlug);
            return false;
        }
    }

    /// <summary>
    /// Invokes a named hook in a loaded plugin. First checks the contento event bus,
    /// then falls back to checking for a global function.
    /// </summary>
    public string? InvokeHook(string pluginSlug, string hookName, string? contextJson = null)
    {
        if (!_engines.TryGetValue(pluginSlug, out var engine))
        {
            _logger.LogWarning("Plugin not loaded: {Slug}", pluginSlug);
            return null;
        }

        try
        {
            // Set up the context data
            if (contextJson != null)
            {
                engine.SetValue("__hookData__", contextJson);
                engine.Execute("var __hookContext__ = JSON.parse(__hookData__);");
            }
            else
            {
                engine.Execute("var __hookContext__ = {};");
            }

            // Invoke hooks registered via contento.on()
            var escapedHookName = hookName.Replace("'", "\\'");
            var fallbackFn = hookName.Replace(":", "_");
            var js = "(function() {" +
                "var hooks = contento._hooks['" + escapedHookName + "'] || [];" +
                "if (hooks.length === 0) {" +
                "  if (typeof " + fallbackFn + " === 'function') {" +
                "    var r = " + fallbackFn + "(__hookContext__);" +
                "    return r == null ? null : (typeof r === 'string' ? r : JSON.stringify(r));" +
                "  }" +
                "  return null;" +
                "}" +
                "var result = __hookContext__;" +
                "for (var i = 0; i < hooks.length; i++) {" +
                "  result = hooks[i](result) || result;" +
                "}" +
                "return typeof result === 'string' ? result : JSON.stringify(result);" +
                "})()";
            var result = engine.Evaluate(js);

            if (result.IsUndefined() || result.IsNull())
                return null;

            return result.IsString() ? result.AsString() : result.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hook invocation failed: {Slug}.{Hook}", pluginSlug, hookName);
            return null;
        }
    }

    /// <summary>
    /// Broadcasts a hook to all loaded plugins.
    /// </summary>
    public Dictionary<string, string?> BroadcastHook(string hookName, string? contextJson = null)
    {
        var results = new Dictionary<string, string?>();
        foreach (var slug in _engines.Keys.ToList())
        {
            var result = InvokeHook(slug, hookName, contextJson);
            if (result != null)
                results[slug] = result;
        }
        return results;
    }

    /// <summary>
    /// Unloads a plugin and disposes its engine.
    /// </summary>
    public bool UnloadPlugin(string pluginSlug)
    {
        if (_engines.Remove(pluginSlug))
        {
            _logger.LogInformation("Plugin unloaded: {Slug}", pluginSlug);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the slugs of all currently loaded plugins.
    /// </summary>
    public IReadOnlyList<string> GetLoadedPlugins() => _engines.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Initializes all enabled plugins for a site from the database.
    /// </summary>
    public async Task InitializeAsync(Guid siteId)
    {
        if (_pluginService == null)
        {
            _logger.LogWarning("PluginService not available, skipping plugin initialization");
            return;
        }

        try
        {
            var plugins = await _pluginService.GetAllBySiteAsync(siteId, enabledOnly: true);
            foreach (var plugin in plugins)
            {
                var jsPath = Path.Combine("plugins", plugin.Slug, "plugin.js");
                if (!File.Exists(jsPath))
                {
                    _logger.LogWarning("Plugin JS not found: {Path}", jsPath);
                    continue;
                }

                var code = await File.ReadAllTextAsync(jsPath);
                var settings = await _pluginService.GetSettingsAsync(plugin.Id);
                LoadPlugin(plugin.Slug, code, settings);
            }

            _logger.LogInformation("Plugin initialization complete: {Count} plugins loaded for site {SiteId}",
                _engines.Count, siteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin initialization failed for site {SiteId}", siteId);
        }
    }

    private static Engine CreateSandboxedEngine()
    {
        return new Engine(options =>
        {
            options.LimitMemory(MaxMemoryMb * 1024 * 1024);
            options.TimeoutInterval(TimeSpan.FromMilliseconds(MaxExecutionMs));
            options.MaxStatements(MaxStatements);
            options.Strict();
        });
    }
}
