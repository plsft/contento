using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for importing and exporting site content.
/// Supports WordPress WXR import, markdown folder import,
/// full site JSON export, and individual post export.
/// </summary>
public interface IImportExportService
{
    /// <summary>
    /// Imports content from a WordPress WXR (XML) file.
    /// Processes posts, pages, categories, tags, and comments.
    /// </summary>
    /// <param name="siteId">The target site identifier.</param>
    /// <param name="wxrStream">The WXR file content stream.</param>
    /// <param name="importedBy">The user performing the import.</param>
    /// <returns>A summary of the import operation including counts of imported items.</returns>
    Task<ImportResult> ImportWordPressAsync(Guid siteId, Stream wxrStream, Guid importedBy);

    /// <summary>
    /// Imports posts from a collection of markdown files.
    /// Front matter (YAML) is parsed for metadata (title, date, tags, categories).
    /// </summary>
    /// <param name="siteId">The target site identifier.</param>
    /// <param name="markdownFiles">Collection of (filename, content) tuples.</param>
    /// <param name="importedBy">The user performing the import.</param>
    /// <returns>A summary of the import operation.</returns>
    Task<ImportResult> ImportMarkdownAsync(Guid siteId, IEnumerable<(string Filename, string Content)> markdownFiles,
        Guid importedBy);

    /// <summary>
    /// Exports the entire site as a JSON document including all posts, categories,
    /// comments, layouts, themes, and settings.
    /// </summary>
    /// <param name="siteId">The site identifier to export.</param>
    /// <returns>The JSON export as a string.</returns>
    Task<string> ExportSiteJsonAsync(Guid siteId);

    /// <summary>
    /// Exports a single post as markdown with front matter metadata.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <returns>The markdown content with YAML front matter.</returns>
    Task<string> ExportPostMarkdownAsync(Guid postId);

    /// <summary>
    /// Exports a single post as rendered HTML with styles.
    /// </summary>
    /// <param name="postId">The post identifier.</param>
    /// <returns>The full HTML document.</returns>
    Task<string> ExportPostHtmlAsync(Guid postId);
}

/// <summary>
/// Result of an import operation.
/// </summary>
public class ImportResult
{
    public int PostsImported { get; set; }
    public int CategoriesImported { get; set; }
    public int TagsCreated { get; set; }
    public int CommentsImported { get; set; }
    public int MediaImported { get; set; }
    public int Errors { get; set; }
    public List<string> ErrorMessages { get; set; } = [];
    public TimeSpan Duration { get; set; }
}
