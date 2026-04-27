namespace Contento.Core.Interfaces;

/// <summary>
/// Service for building internal cross-links between pSEO pages within a collection.
/// </summary>
public interface IInternalLinkingService
{
    /// <summary>
    /// Builds internal links for all published pages in a collection.
    /// </summary>
    /// <param name="collectionId">The collection to build links for.</param>
    /// <param name="linksPerPage">Maximum number of related links to inject per page.</param>
    Task BuildLinksAsync(Guid collectionId, int linksPerPage = 3);

    /// <summary>
    /// Builds internal links for a single page.
    /// </summary>
    /// <param name="pageId">The page to build links for.</param>
    /// <param name="maxLinks">Maximum number of related links to inject.</param>
    Task BuildLinksForPageAsync(Guid pageId, int maxLinks = 3);
}
