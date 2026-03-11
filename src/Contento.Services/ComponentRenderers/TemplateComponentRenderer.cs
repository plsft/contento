using System.Text.RegularExpressions;
using Contento.Core.Interfaces;

namespace Contento.Services.ComponentRenderers;

public partial class TemplateComponentRenderer : IComponentRenderer
{
    private readonly string _templatesPath;

    public TemplateComponentRenderer(string templatesPath)
    {
        _templatesPath = templatesPath;
    }

    public string ContentType => "template";

    public string Render(LayoutComponentContext context)
    {
        var templateName = context.Content?.Trim();
        if (string.IsNullOrEmpty(templateName))
            return "<!-- template: no template name specified -->";

        // Sanitize: only allow alphanumeric, hyphens, underscores
        if (!SafeNameRegex().IsMatch(templateName))
            return $"<!-- template: invalid template name '{templateName}' -->";

        var templatePath = Path.Combine(_templatesPath, $"{templateName}.html");
        if (!File.Exists(templatePath))
            return $"<!-- template: '{templateName}' not found -->";

        return File.ReadAllText(templatePath);
    }

    [GeneratedRegex(@"^[a-zA-Z0-9_-]+$")]
    private static partial Regex SafeNameRegex();
}
