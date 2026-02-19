using System.Data;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;

namespace Contento.Services;

/// <summary>
/// Service for managing media library assets with CRUD, paginated listing,
/// MIME type filtering, and file URL resolution.
/// </summary>
public class MediaService : IMediaService
{
    private readonly IDbConnection _db;
    private readonly IFileStorageService? _fileStorage;
    private readonly ILogger<MediaService> _logger;

    public MediaService(IDbConnection db, ILogger<MediaService> logger, IFileStorageService? fileStorage = null)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
        _fileStorage = fileStorage;
    }

    /// <inheritdoc />
    public async Task<Media?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<Media>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Media>> GetAllBySiteAsync(Guid siteId, string? mimeTypeFilter = null,
        Guid? uploadedBy = null, int page = 1, int pageSize = 30)
    {
        Guard.Against.Default(siteId);

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        var conditions = new List<string> { "site_id = @SiteId" };
        var parameters = new Dictionary<string, object> { { "SiteId", siteId } };

        if (!string.IsNullOrWhiteSpace(mimeTypeFilter))
        {
            conditions.Add("mime_type LIKE @MimeFilter");
            parameters["MimeFilter"] = mimeTypeFilter + "%";
        }

        if (uploadedBy.HasValue && uploadedBy.Value != Guid.Empty)
        {
            conditions.Add("uploaded_by = @UploadedBy");
            parameters["UploadedBy"] = uploadedBy.Value;
        }

        parameters["Limit"] = pageSize;
        parameters["Offset"] = offset;

        var sql = $@"SELECT * FROM media
                     WHERE {string.Join(" AND ", conditions)}
                     ORDER BY created_at DESC
                     LIMIT @Limit OFFSET @Offset";

        return await _db.QueryAsync<Media>(sql, parameters);
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(Guid siteId, string? mimeTypeFilter = null)
    {
        Guard.Against.Default(siteId);

        if (!string.IsNullOrWhiteSpace(mimeTypeFilter))
        {
            return await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM media WHERE site_id = @SiteId AND mime_type LIKE @MimeFilter",
                new { SiteId = siteId, MimeFilter = mimeTypeFilter + "%" });
        }

        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM media WHERE site_id = @SiteId",
            new { SiteId = siteId });
    }

    /// <inheritdoc />
    public async Task<Media> UploadAsync(Guid siteId, Guid uploadedBy, string originalName,
        string mimeType, Stream fileStream)
    {
        Guard.Against.Default(siteId);
        Guard.Against.Default(uploadedBy);
        Guard.Against.NullOrWhiteSpace(originalName);
        Guard.Against.NullOrWhiteSpace(mimeType);
        Guard.Against.Null(fileStream);

        var id = Guid.NewGuid();
        var extension = Path.GetExtension(originalName);
        var filename = $"{id}{extension}";
        var storagePath = $"media/{siteId}/{filename}";

        // Write file to storage
        if (_fileStorage != null)
        {
            await _fileStorage.UploadAsync(fileStream, storagePath);
        }

        var media = new Media
        {
            Id = id,
            SiteId = siteId,
            Filename = filename,
            OriginalName = originalName,
            MimeType = mimeType,
            FileSize = fileStream.Length,
            StoragePath = storagePath,
            UploadedBy = uploadedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(media);

        // Generate thumbnail and extract dimensions for image files
        if (mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                fileStream.Position = 0;
                using var image = await Image.LoadAsync(fileStream);

                // Read dimensions
                media.Width = image.Width;
                media.Height = image.Height;

                // Strip EXIF metadata for privacy
                image.Metadata.ExifProfile = null;

                // Generate thumbnail: 400px wide, maintain aspect ratio
                var thumbnailFilename = $"{id}_thumb.webp";
                var thumbnailPath = $"media/{siteId}/{thumbnailFilename}";

                var ratio = 400.0 / image.Width;
                var thumbHeight = (int)Math.Round(image.Height * ratio);

                image.Mutate(x => x.Resize(400, thumbHeight));

                if (_fileStorage != null)
                {
                    using var thumbStream = new MemoryStream();
                    await image.SaveAsWebpAsync(thumbStream);
                    thumbStream.Position = 0;
                    await _fileStorage.UploadAsync(thumbStream, thumbnailPath);
                }

                media.ThumbnailPath = thumbnailPath;

                // Generate responsive variants (640w, 1024w, 1536w) for srcset
                fileStream.Position = 0;
                using var originalForVariants = await Image.LoadAsync(fileStream);
                originalForVariants.Metadata.ExifProfile = null;

                int[] widths = [640, 1024, 1536];
                foreach (var targetWidth in widths)
                {
                    if (originalForVariants.Width <= targetWidth)
                        continue; // Skip variants larger than original

                    try
                    {
                        using var variant = originalForVariants.Clone(x =>
                        {
                            var variantRatio = (double)targetWidth / originalForVariants.Width;
                            var variantHeight = (int)Math.Round(originalForVariants.Height * variantRatio);
                            x.Resize(targetWidth, variantHeight);
                        });

                        var variantFilename = $"{id}_{targetWidth}w.webp";
                        var variantPath = $"media/{siteId}/{variantFilename}";

                        if (_fileStorage != null)
                        {
                            using var variantStream = new MemoryStream();
                            await variant.SaveAsWebpAsync(variantStream);
                            variantStream.Position = 0;
                            await _fileStorage.UploadAsync(variantStream, variantPath);
                        }

                        _logger.LogDebug("Generated {Width}w variant for {Filename}", targetWidth, originalName);
                    }
                    catch (Exception variantEx)
                    {
                        _logger.LogWarning(variantEx, "Failed to generate {Width}w variant for {Filename}", targetWidth, originalName);
                    }
                }

                // Persist width, height, and thumbnail path
                await _db.UpdateAsync(media);
                _logger.LogInformation("Thumbnail generated for {Filename}: {ThumbnailPath}", originalName, thumbnailPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process image {Filename}; upload saved without thumbnail", originalName);
            }
        }

        _logger.LogInformation("Media uploaded: {Filename} ({MimeType})", originalName, mimeType);
        return media;
    }

    /// <inheritdoc />
    public async Task<Media> UpdateAsync(Media media)
    {
        Guard.Against.Null(media);
        Guard.Against.Default(media.Id);

        await _db.UpdateAsync(media);
        return media;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        var media = await _db.GetAsync<Media>(id);
        if (media != null)
        {
            // Remove physical file from storage
            if (_fileStorage != null && !string.IsNullOrWhiteSpace(media.StoragePath))
            {
                await _fileStorage.DeleteAsync(media.StoragePath);
            }
            // Remove thumbnail from storage
            if (_fileStorage != null && !string.IsNullOrWhiteSpace(media.ThumbnailPath))
            {
                await _fileStorage.DeleteAsync(media.ThumbnailPath);
            }
            await _db.DeleteAsync(media);
        }
    }

    /// <inheritdoc />
    public string GetPublicUrl(string storagePath)
    {
        Guard.Against.NullOrWhiteSpace(storagePath);
        if (_fileStorage != null)
            return _fileStorage.GetPublicUrl(storagePath);
        return $"/uploads/{storagePath}";
    }

    /// <inheritdoc />
    public string? GetThumbnailUrl(string? thumbnailPath)
    {
        if (string.IsNullOrWhiteSpace(thumbnailPath))
            return null;

        if (_fileStorage != null)
            return _fileStorage.GetPublicUrl(thumbnailPath);
        return $"/uploads/{thumbnailPath}";
    }
}
