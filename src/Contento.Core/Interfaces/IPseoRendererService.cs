using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for rendering pSEO pages into full HTML output with project chrome and schema-driven content.
/// </summary>
public interface IPseoRendererService
{
    /// <summary>
    /// Renders a complete HTML page including project chrome (header, footer, CSS) and schema-driven content.
    /// </summary>
    /// <param name="page">The pSEO page to render.</param>
    /// <param name="project">The project providing chrome (header/footer HTML, custom CSS).</param>
    /// <param name="schema">The content schema defining the rendering template.</param>
    /// <returns>The fully rendered HTML string.</returns>
    Task<string> RenderPageAsync(PseoPage page, PseoProject project, ContentSchema schema);

    /// <summary>
    /// Renders just the content portion of a page without project chrome, useful for previews.
    /// </summary>
    /// <param name="page">The pSEO page to render.</param>
    /// <param name="schema">The content schema defining the rendering template.</param>
    /// <returns>The rendered content HTML string.</returns>
    Task<string> RenderContentAsync(PseoPage page, ContentSchema schema);
}
