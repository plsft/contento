using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing media library assets (images, files, etc.).
/// Handles CRUD operations, file upload processing, and paginated listing.
/// </summary>
public interface IMediaService
{
    /// <summary>
    /// Retrieves a media asset by its unique identifier.
    /// </summary>
    /// <param name="id">The media identifier.</param>
    /// <returns>The media asset if found; otherwise null.</returns>
    Task<Media?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves all media assets for a site with pagination and optional filtering.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="mimeTypeFilter">Optional MIME type prefix filter (e.g., "image/", "video/").</param>
    /// <param name="uploadedBy">Optional uploader user identifier filter.</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of media assets.</returns>
    Task<IEnumerable<Media>> GetAllBySiteAsync(Guid siteId, string? mimeTypeFilter = null,
        Guid? uploadedBy = null, int page = 1, int pageSize = 30);

    /// <summary>
    /// Returns the total count of media assets for a site with optional filtering.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="mimeTypeFilter">Optional MIME type prefix filter.</param>
    /// <returns>The total count of matching media assets.</returns>
    Task<int> GetTotalCountAsync(Guid siteId, string? mimeTypeFilter = null);

    /// <summary>
    /// Uploads a new media file. Validates MIME type and file size,
    /// generates a storage path, creates thumbnails for images,
    /// and persists the media record.
    /// </summary>
    /// <param name="siteId">The site identifier.</param>
    /// <param name="uploadedBy">The identifier of the user uploading the file.</param>
    /// <param name="originalName">The original filename as provided by the client.</param>
    /// <param name="mimeType">The MIME type of the file.</param>
    /// <param name="fileStream">The file content stream.</param>
    /// <returns>The created media record.</returns>
    Task<Media> UploadAsync(Guid siteId, Guid uploadedBy, string originalName, string mimeType, Stream fileStream);

    /// <summary>
    /// Updates the metadata of an existing media asset (alt text, caption).
    /// </summary>
    /// <param name="media">The media asset with updated metadata fields.</param>
    /// <returns>The updated media asset.</returns>
    Task<Media> UpdateAsync(Media media);

    /// <summary>
    /// Deletes a media asset. Removes the file from storage
    /// and its thumbnail if one exists.
    /// </summary>
    /// <param name="id">The media identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Returns the public URL for a media asset's stored file.
    /// </summary>
    /// <param name="storagePath">The storage path of the media file.</param>
    /// <returns>The public URL string.</returns>
    string GetPublicUrl(string storagePath);

    /// <summary>
    /// Returns the public URL for a media asset's thumbnail.
    /// </summary>
    /// <param name="thumbnailPath">The storage path of the thumbnail.</param>
    /// <returns>The public URL string, or null if no thumbnail exists.</returns>
    string? GetThumbnailUrl(string? thumbnailPath);
}
