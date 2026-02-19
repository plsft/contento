using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for full-text search across posts using PostgreSQL tsvector and trigram matching.
/// Supports ranked results, highlighting, and typeahead suggestions.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Performs a full-text search across published posts for a site.
    /// Searches title, body markdown, and tags. Results are ranked by relevance.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="query">The search query text.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of posts matching the search query.</returns>
    Task<IEnumerable<Post>> SearchPostsAsync(Guid siteId, string query, int page = 1, int pageSize = 20);

    /// <summary>
    /// Returns the total count of posts matching the search query.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="query">The search query text.</param>
    /// <returns>The number of posts matching the query.</returns>
    Task<int> GetSearchResultCountAsync(Guid siteId, string query);

    /// <summary>
    /// Returns search suggestions (typeahead) based on a partial query.
    /// Uses trigram similarity matching for partial and fuzzy matches.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="partialQuery">The partial search query text.</param>
    /// <param name="limit">Maximum number of suggestions to return.</param>
    /// <returns>A collection of suggested search terms (post titles).</returns>
    Task<IEnumerable<string>> GetSuggestionsAsync(Guid siteId, string partialQuery, int limit = 5);

    /// <summary>
    /// Returns search results with highlighted matching fragments in title and body.
    /// Highlighted terms are wrapped in &lt;mark&gt; tags.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="query">The search query text.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A collection of tuples containing the post and its highlighted title and body excerpt.</returns>
    Task<IEnumerable<(Post Post, string HighlightedTitle, string HighlightedExcerpt)>> SearchWithHighlightsAsync(
        Guid siteId, string query, int page = 1, int pageSize = 20);
}
