using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing custom post type definitions.
/// </summary>
public interface IPostTypeService
{
    Task<PostType?> GetByIdAsync(Guid id);
    Task<PostType?> GetBySlugAsync(Guid siteId, string slug);
    Task<IEnumerable<PostType>> GetAllAsync(Guid siteId);
    Task<PostType> CreateAsync(PostType postType);
    Task<PostType> UpdateAsync(PostType postType);
    Task DeleteAsync(Guid id);
}
