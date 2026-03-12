using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Contento.Plugins;

namespace Contento.Tests.Plugins;

/// <summary>
/// Tests for <see cref="PluginHost"/>. The host uses Jint to execute JavaScript plugins
/// in a sandboxed environment. Tests verify plugin loading, hook invocation, broadcasting,
/// unloading, and sandbox safety limits (memory, time, statement count).
/// </summary>
[TestFixture]
public class PluginHostTests
{
    private Mock<ILogger<PluginHost>> _mockLogger = null!;
    private PluginHost _host = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<PluginHost>>();
        _host = new PluginHost(_mockLogger.Object);
    }

    // ---------------------------------------------------------------
    // LoadPlugin
    // ---------------------------------------------------------------

    [Test]
    public void LoadPlugin_ValidJavaScript_ReturnsTrue()
    {
        var result = _host.LoadPlugin("test-plugin", "function onActivate() { return 'activated'; }");

        Assert.That(result, Is.True);
    }

    [Test]
    public void LoadPlugin_EmptyScript_ReturnsTrue()
    {
        // An empty script is valid JS — it just does nothing
        var result = _host.LoadPlugin("empty-plugin", "");

        Assert.That(result, Is.True);
    }

    [Test]
    public void LoadPlugin_InvalidJavaScript_ReturnsFalse()
    {
        var result = _host.LoadPlugin("bad-plugin", "function broken( {{{ }}}");

        Assert.That(result, Is.False);
    }

    [Test]
    public void LoadPlugin_AppearsInLoadedPlugins()
    {
        _host.LoadPlugin("my-plugin", "var x = 1;");

        var loaded = _host.GetLoadedPlugins();

        Assert.That(loaded, Does.Contain("my-plugin"));
    }

    [Test]
    public void LoadPlugin_MultipleTimes_OverwritesPrevious()
    {
        _host.LoadPlugin("my-plugin", "function greet() { return 'v1'; }");
        _host.LoadPlugin("my-plugin", "function greet() { return 'v2'; }");

        var result = _host.InvokeHook("my-plugin", "greet");

        Assert.That(result, Is.EqualTo("v2"));
    }

    [Test]
    public void LoadPlugin_MultiplePlugins_AllLoaded()
    {
        _host.LoadPlugin("plugin-a", "var a = 1;");
        _host.LoadPlugin("plugin-b", "var b = 2;");
        _host.LoadPlugin("plugin-c", "var c = 3;");

        var loaded = _host.GetLoadedPlugins();

        Assert.That(loaded, Has.Count.EqualTo(3));
        Assert.That(loaded, Does.Contain("plugin-a"));
        Assert.That(loaded, Does.Contain("plugin-b"));
        Assert.That(loaded, Does.Contain("plugin-c"));
    }

    [Test]
    public void LoadPlugin_WithSettings_MakesSettingsAvailable()
    {
        var settings = "{\"color\": \"blue\", \"count\": 5}";
        _host.LoadPlugin("settings-plugin",
            "function getColor() { return PLUGIN_SETTINGS.color; }",
            settings);

        var result = _host.InvokeHook("settings-plugin", "getColor");

        Assert.That(result, Is.EqualTo("blue"));
    }

    [Test]
    public void LoadPlugin_WithNullSettings_DefaultsToEmptyObject()
    {
        _host.LoadPlugin("no-settings-plugin",
            "function getKeys() { return Object.keys(PLUGIN_SETTINGS).length.toString(); }",
            null);

        var result = _host.InvokeHook("no-settings-plugin", "getKeys");

        Assert.That(result, Is.EqualTo("0"));
    }

    [Test]
    public void LoadPlugin_ConsoleLogAvailable()
    {
        // Console.log should be mapped to logger — should not throw
        var result = _host.LoadPlugin("logging-plugin",
            "console.log('Plugin loaded successfully');");

        Assert.That(result, Is.True);
    }

    [Test]
    public void LoadPlugin_ConsoleWarnAvailable()
    {
        var result = _host.LoadPlugin("warn-plugin",
            "console.warn('This is a warning');");

        Assert.That(result, Is.True);
    }

    [Test]
    public void LoadPlugin_ConsoleErrorAvailable()
    {
        var result = _host.LoadPlugin("error-plugin",
            "console.error('This is an error');");

        Assert.That(result, Is.True);
    }

    // ---------------------------------------------------------------
    // InvokeHook
    // ---------------------------------------------------------------

    [Test]
    public void InvokeHook_ExistingFunction_ReturnsResult()
    {
        _host.LoadPlugin("greet-plugin", "function greet() { return 'Hello!'; }");

        var result = _host.InvokeHook("greet-plugin", "greet");

        Assert.That(result, Is.EqualTo("Hello!"));
    }

    [Test]
    public void InvokeHook_WithData_PassesJsonToFunction()
    {
        _host.LoadPlugin("data-plugin",
            "function process(data) { return 'Name: ' + data.name; }");

        var result = _host.InvokeHook("data-plugin", "process", "{\"name\": \"Alice\"}");

        Assert.That(result, Is.EqualTo("Name: Alice"));
    }

    [Test]
    public void InvokeHook_WithNestedJsonData_ParsesCorrectly()
    {
        _host.LoadPlugin("nested-plugin",
            "function process(data) { return data.user.email; }");

        var result = _host.InvokeHook("nested-plugin", "process",
            "{\"user\": {\"email\": \"alice@example.com\"}}");

        Assert.That(result, Is.EqualTo("alice@example.com"));
    }

    [Test]
    public void InvokeHook_NonExistentFunction_ReturnsNull()
    {
        _host.LoadPlugin("some-plugin", "var x = 1;");

        var result = _host.InvokeHook("some-plugin", "nonExistentHook");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InvokeHook_NonExistentPlugin_ReturnsNull()
    {
        var result = _host.InvokeHook("nonexistent-plugin", "someHook");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InvokeHook_ReturnsUndefined_ReturnsNull()
    {
        _host.LoadPlugin("void-plugin", "function noReturn() { }");

        var result = _host.InvokeHook("void-plugin", "noReturn");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InvokeHook_ReturnsNull_ReturnsNull()
    {
        _host.LoadPlugin("null-plugin", "function getNull() { return null; }");

        var result = _host.InvokeHook("null-plugin", "getNull");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InvokeHook_ReturnsNumber_ConvertsToString()
    {
        _host.LoadPlugin("number-plugin", "function getNumber() { return 42; }");

        var result = _host.InvokeHook("number-plugin", "getNumber");

        Assert.That(result, Is.EqualTo("42"));
    }

    [Test]
    public void InvokeHook_ReturnsBoolTrue_ConvertsToString()
    {
        _host.LoadPlugin("bool-plugin", "function getBool() { return true; }");

        var result = _host.InvokeHook("bool-plugin", "getBool");

        // Jint's ToString on a boolean
        Assert.That(result?.ToLowerInvariant(), Is.EqualTo("true"));
    }

    [Test]
    public void InvokeHook_FunctionThrowsError_ReturnsNull()
    {
        _host.LoadPlugin("error-plugin", "function explode() { throw new Error('boom'); }");

        var result = _host.InvokeHook("error-plugin", "explode");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void InvokeHook_WithoutData_CallsFunctionWithNoArgs()
    {
        _host.LoadPlugin("noargs-plugin",
            "function getTime() { return 'no-args-called'; }");

        var result = _host.InvokeHook("noargs-plugin", "getTime");

        Assert.That(result, Is.EqualTo("no-args-called"));
    }

    [Test]
    public void InvokeHook_StringReturn_ReturnsExactString()
    {
        _host.LoadPlugin("str-plugin",
            "function getMessage() { return 'Hello, World!'; }");

        var result = _host.InvokeHook("str-plugin", "getMessage");

        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void InvokeHook_ComplexLogic_ExecutesCorrectly()
    {
        var code = @"
            function transform(data) {
                var words = data.text.split(' ');
                return words.map(function(w) { return w.toUpperCase(); }).join('-');
            }
        ";
        _host.LoadPlugin("transform-plugin", code);

        var result = _host.InvokeHook("transform-plugin", "transform",
            "{\"text\": \"hello world\"}");

        Assert.That(result, Is.EqualTo("HELLO-WORLD"));
    }

    // ---------------------------------------------------------------
    // BroadcastHook
    // ---------------------------------------------------------------

    [Test]
    public void BroadcastHook_NoPlugins_ReturnsEmptyDictionary()
    {
        var results = _host.BroadcastHook("onSave");

        Assert.That(results, Is.Empty);
    }

    [Test]
    public void BroadcastHook_MultiplePlugins_AllReceiveHook()
    {
        _host.LoadPlugin("plugin-a", "function onSave() { return 'A saved'; }");
        _host.LoadPlugin("plugin-b", "function onSave() { return 'B saved'; }");

        var results = _host.BroadcastHook("onSave");

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results["plugin-a"], Is.EqualTo("A saved"));
        Assert.That(results["plugin-b"], Is.EqualTo("B saved"));
    }

    [Test]
    public void BroadcastHook_OnlyPluginsWithHook_ReturnResults()
    {
        _host.LoadPlugin("has-hook", "function onPublish() { return 'published'; }");
        _host.LoadPlugin("no-hook", "var x = 1;"); // does not define onPublish

        var results = _host.BroadcastHook("onPublish");

        // Only the plugin that defines the hook should appear in results
        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results["has-hook"], Is.EqualTo("published"));
    }

    [Test]
    public void BroadcastHook_WithData_PassesDataToAllPlugins()
    {
        _host.LoadPlugin("plugin-x", "function onEvent(data) { return 'x:' + data.type; }");
        _host.LoadPlugin("plugin-y", "function onEvent(data) { return 'y:' + data.type; }");

        var results = _host.BroadcastHook("onEvent", "{\"type\": \"click\"}");

        Assert.That(results["plugin-x"], Is.EqualTo("x:click"));
        Assert.That(results["plugin-y"], Is.EqualTo("y:click"));
    }

    [Test]
    public void BroadcastHook_PluginThrows_OtherPluginsStillExecute()
    {
        _host.LoadPlugin("good-plugin", "function onAction() { return 'OK'; }");
        _host.LoadPlugin("bad-plugin", "function onAction() { throw new Error('fail'); }");

        var results = _host.BroadcastHook("onAction");

        // The good plugin should still return its result
        Assert.That(results.ContainsKey("good-plugin"), Is.True);
        Assert.That(results["good-plugin"], Is.EqualTo("OK"));
    }

    // ---------------------------------------------------------------
    // UnloadPlugin
    // ---------------------------------------------------------------

    [Test]
    public void UnloadPlugin_LoadedPlugin_ReturnsTrue()
    {
        _host.LoadPlugin("removable", "var x = 1;");

        var result = _host.UnloadPlugin("removable");

        Assert.That(result, Is.True);
    }

    [Test]
    public void UnloadPlugin_RemovedFromLoadedPlugins()
    {
        _host.LoadPlugin("removable", "var x = 1;");
        _host.UnloadPlugin("removable");

        var loaded = _host.GetLoadedPlugins();

        Assert.That(loaded, Does.Not.Contain("removable"));
    }

    [Test]
    public void UnloadPlugin_NonExistentPlugin_ReturnsFalse()
    {
        var result = _host.UnloadPlugin("does-not-exist");

        Assert.That(result, Is.False);
    }

    [Test]
    public void UnloadPlugin_HookInvocationAfterUnload_ReturnsNull()
    {
        _host.LoadPlugin("temp-plugin", "function greet() { return 'hi'; }");
        _host.UnloadPlugin("temp-plugin");

        var result = _host.InvokeHook("temp-plugin", "greet");

        Assert.That(result, Is.Null);
    }

    [Test]
    public void UnloadPlugin_DoesNotAffectOtherPlugins()
    {
        _host.LoadPlugin("keep", "function test() { return 'kept'; }");
        _host.LoadPlugin("remove", "function test() { return 'removed'; }");

        _host.UnloadPlugin("remove");

        Assert.That(_host.GetLoadedPlugins(), Does.Contain("keep"));
        Assert.That(_host.InvokeHook("keep", "test"), Is.EqualTo("kept"));
    }

    // ---------------------------------------------------------------
    // GetLoadedPlugins
    // ---------------------------------------------------------------

    [Test]
    public void GetLoadedPlugins_InitiallyEmpty()
    {
        var loaded = _host.GetLoadedPlugins();

        Assert.That(loaded, Is.Empty);
    }

    [Test]
    public void GetLoadedPlugins_ReturnsReadOnlyList()
    {
        _host.LoadPlugin("p1", "var a=1;");

        var loaded = _host.GetLoadedPlugins();

        Assert.That(loaded, Is.InstanceOf<IReadOnlyList<string>>());
    }

    // ---------------------------------------------------------------
    // Sandboxing limits
    // ---------------------------------------------------------------

    [Test]
    public void Sandbox_InfiniteLoop_ThrowsOrReturnsFalse()
    {
        // The Jint engine has MaxStatements(100_000) and TimeoutInterval(5000ms).
        // An infinite loop should be terminated.
        var result = _host.LoadPlugin("infinite-plugin", "while(true) {}");

        // LoadPlugin catches exceptions and returns false
        Assert.That(result, Is.False);
    }

    [Test]
    public void Sandbox_ExcessiveStatements_TerminatesExecution()
    {
        // Generate code that runs way more than 100,000 statements
        var code = "var sum = 0; for (var i = 0; i < 1000000; i++) { sum += i; }";
        var result = _host.LoadPlugin("excessive-plugin", code);

        Assert.That(result, Is.False);
    }

    [Test]
    public void Sandbox_HookWithInfiniteLoop_ReturnsNull()
    {
        _host.LoadPlugin("hook-loop-plugin",
            "function runForever() { while(true) {} return 'done'; }");

        var result = _host.InvokeHook("hook-loop-plugin", "runForever");

        // InvokeHook catches the timeout exception and returns null
        Assert.That(result, Is.Null);
    }

    [Test]
    public void Sandbox_StrictMode_IsEnabled()
    {
        // Strict mode: assigning to an undeclared variable should throw
        // The engine is configured with options.Strict()
        var result = _host.LoadPlugin("strict-plugin",
            "undeclaredVariable = 42;");

        // In strict mode, this should fail during execution
        Assert.That(result, Is.False);
    }

    [Test]
    public void Sandbox_NoFileSystemAccess()
    {
        // There is no 'require' or 'fs' in Jint — this should fail or have no effect
        var result = _host.LoadPlugin("fs-plugin",
            "var fs = require('fs');");

        Assert.That(result, Is.False);
    }

    [Test]
    public void Sandbox_NoProcessAccess()
    {
        // 'process' is not defined in Jint
        _host.LoadPlugin("process-plugin",
            "function getEnv() { return typeof process; }");

        var result = _host.InvokeHook("process-plugin", "getEnv");

        Assert.That(result, Is.EqualTo("undefined"));
    }

    [Test]
    public void Sandbox_ReasonableCode_ExecutesSuccessfully()
    {
        // Normal code that stays within limits should work fine
        var code = @"
            function fibonacci(n) {
                if (n <= 1) return n;
                var a = 0, b = 1;
                for (var i = 2; i <= n; i++) {
                    var temp = b;
                    b = a + b;
                    a = temp;
                }
                return b.toString();
            }
        ";
        var loaded = _host.LoadPlugin("fib-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("fib-plugin", "fibonacci", "10");

        Assert.That(result, Is.EqualTo("55"));
    }

    [Test]
    public void Sandbox_PluginIsolation_EnginesAreIndependent()
    {
        // Variables defined in one plugin should not leak to another
        _host.LoadPlugin("plugin-1", "var secret = 'hidden';");
        _host.LoadPlugin("plugin-2", "function getSecret() { return typeof secret; }");

        var result = _host.InvokeHook("plugin-2", "getSecret");

        Assert.That(result, Is.EqualTo("undefined"));
    }

    [Test]
    public void Sandbox_JsonStringifyAvailable()
    {
        _host.LoadPlugin("json-plugin",
            "function toJson() { return JSON.stringify({a: 1, b: 'hello'}); }");

        var result = _host.InvokeHook("json-plugin", "toJson");

        Assert.That(result, Does.Contain("\"a\":1"));
        Assert.That(result, Does.Contain("\"b\":\"hello\""));
    }

    [Test]
    public void Sandbox_ArrayMethodsAvailable()
    {
        _host.LoadPlugin("array-plugin",
            "function sum() { return [1,2,3,4,5].reduce(function(a,b) { return a+b; }, 0).toString(); }");

        var result = _host.InvokeHook("array-plugin", "sum");

        Assert.That(result, Is.EqualTo("15"));
    }

    [Test]
    public void Sandbox_StringMethodsAvailable()
    {
        _host.LoadPlugin("string-plugin",
            "function upper(data) { return data.text.toUpperCase(); }");

        var result = _host.InvokeHook("string-plugin", "upper", "{\"text\": \"hello\"}");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    // ---------------------------------------------------------------
    // ES6+ Feature Support (Jint 4.6)
    // ---------------------------------------------------------------

    [Test]
    public void ES6_ArrowFunctions_Supported()
    {
        var code = "const greet = (name) => `Hello, ${name}!`; function test(data) { return greet(data.name); }";
        var loaded = _host.LoadPlugin("arrow-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("arrow-plugin", "test", "{\"name\": \"World\"}");
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void ES6_ConstAndLet_Supported()
    {
        var code = @"
            function test() {
                const x = 10;
                let y = 20;
                y = 30;
                return (x + y).toString();
            }
        ";
        var loaded = _host.LoadPlugin("const-let-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("const-let-plugin", "test");
        Assert.That(result, Is.EqualTo("40"));
    }

    [Test]
    public void ES6_TemplateLiterals_Supported()
    {
        var code = @"
            function test(data) {
                const { name, age } = data;
                return `${name} is ${age} years old`;
            }
        ";
        var loaded = _host.LoadPlugin("template-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("template-plugin", "test", "{\"name\": \"Alice\", \"age\": 30}");
        Assert.That(result, Is.EqualTo("Alice is 30 years old"));
    }

    [Test]
    public void ES6_Destructuring_Supported()
    {
        var code = @"
            function test(data) {
                const { a, b, c } = data;
                const [first, ...rest] = [a, b, c];
                return `${first}-${rest.join(',')}`;
            }
        ";
        var loaded = _host.LoadPlugin("destruct-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("destruct-plugin", "test", "{\"a\": 1, \"b\": 2, \"c\": 3}");
        Assert.That(result, Is.EqualTo("1-2,3"));
    }

    [Test]
    public void ES6_SpreadOperator_Supported()
    {
        var code = @"
            function test(data) {
                const arr1 = [1, 2, 3];
                const arr2 = [...arr1, 4, 5];
                return arr2.join(',');
            }
        ";
        var loaded = _host.LoadPlugin("spread-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("spread-plugin", "test", "{}");
        Assert.That(result, Is.EqualTo("1,2,3,4,5"));
    }

    [Test]
    public void ES6_Classes_Supported()
    {
        var code = @"
            class Greeter {
                constructor(prefix) {
                    this.prefix = prefix;
                }
                greet(name) {
                    return `${this.prefix}, ${name}!`;
                }
            }
            function test(data) {
                const g = new Greeter('Hello');
                return g.greet(data.name);
            }
        ";
        var loaded = _host.LoadPlugin("class-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("class-plugin", "test", "{\"name\": \"Bob\"}");
        Assert.That(result, Is.EqualTo("Hello, Bob!"));
    }

    [Test]
    public void ES6_DefaultParameters_Supported()
    {
        var code = @"
            function greet(name = 'World') {
                return `Hello, ${name}!`;
            }
            function test() {
                return greet();
            }
        ";
        var loaded = _host.LoadPlugin("defaults-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("defaults-plugin", "test");
        Assert.That(result, Is.EqualTo("Hello, World!"));
    }

    [Test]
    public void ES6_Promises_Supported()
    {
        // Jint supports Promise construction (sync resolution)
        var code = @"
            function test() {
                const p = new Promise((resolve) => resolve('done'));
                return typeof p === 'object' ? 'promise-exists' : 'no-promise';
            }
        ";
        var loaded = _host.LoadPlugin("promise-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("promise-plugin", "test");
        Assert.That(result, Is.EqualTo("promise-exists"));
    }

    [Test]
    public void ES6_Map_And_Set_Supported()
    {
        var code = @"
            function test() {
                const map = new Map();
                map.set('key', 'value');
                const set = new Set([1, 2, 2, 3]);
                return `map:${map.get('key')},set:${set.size}`;
            }
        ";
        var loaded = _host.LoadPlugin("collections-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("collections-plugin", "test");
        Assert.That(result, Is.EqualTo("map:value,set:3"));
    }

    [Test]
    public void ES6_ForOf_Supported()
    {
        var code = @"
            function test() {
                const items = ['a', 'b', 'c'];
                let result = '';
                for (const item of items) {
                    result += item;
                }
                return result;
            }
        ";
        var loaded = _host.LoadPlugin("forof-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("forof-plugin", "test");
        Assert.That(result, Is.EqualTo("abc"));
    }

    [Test]
    public void ES6_Symbol_Supported()
    {
        var code = @"
            function test() {
                const sym = Symbol('test');
                return typeof sym;
            }
        ";
        var loaded = _host.LoadPlugin("symbol-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("symbol-plugin", "test");
        Assert.That(result, Is.EqualTo("symbol"));
    }

    [Test]
    public void ES2020_OptionalChaining_Supported()
    {
        var code = @"
            function test(data) {
                const val = data?.nested?.deep?.value ?? 'fallback';
                return val;
            }
        ";
        var loaded = _host.LoadPlugin("optional-chain-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("optional-chain-plugin", "test", "{\"nested\": null}");
        Assert.That(result, Is.EqualTo("fallback"));
    }

    [Test]
    public void ES2020_NullishCoalescing_Supported()
    {
        var code = @"
            function test() {
                const a = null ?? 'default';
                const b = 0 ?? 'default';
                return `${a},${b}`;
            }
        ";
        var loaded = _host.LoadPlugin("nullish-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("nullish-plugin", "test");
        Assert.That(result, Is.EqualTo("default,0"));
    }

    [Test]
    public void ES6_ArrowFunctionPlugin_WithEventBus_Works()
    {
        // This tests the exact pattern from our marketing docs
        var code = @"
            (() => {
                contento.on('post:render', (context) => {
                    const { title, slug } = context;
                    return `Rendered: ${title} at /${slug}`;
                });
            })();
        ";
        var loaded = _host.LoadPlugin("modern-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("modern-plugin", "post:render",
            "{\"title\": \"My Post\", \"slug\": \"my-post\"}");
        Assert.That(result, Is.EqualTo("Rendered: My Post at /my-post"));
    }

    [Test]
    public void ES6_ArrayMethods_Modern_Supported()
    {
        var code = @"
            function test() {
                const nums = [1, 2, 3, 4, 5];
                const doubled = nums.map(n => n * 2);
                const even = doubled.filter(n => n > 4);
                const sum = even.reduce((a, b) => a + b, 0);
                return sum.toString();
            }
        ";
        var loaded = _host.LoadPlugin("modern-array-plugin", code);
        Assert.That(loaded, Is.True);

        var result = _host.InvokeHook("modern-array-plugin", "test");
        Assert.That(result, Is.EqualTo("24"));  // [6, 8, 10] => 24
    }
}
