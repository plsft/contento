using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Web.Middleware;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Contento.Web.Controllers;

[Tags("Media")]
[ApiController]
[Route("api/v1/media")]
[Authorize(AuthenticationSchemes = "Bearer,Cookies")]
public class MediaApiController : ControllerBase
{
    private readonly IMediaService _mediaService;
    private readonly ISiteService _siteService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
        "application/pdf", "video/mp4"
    };

    public MediaApiController(IMediaService mediaService, ISiteService siteService,
        IFileStorageService fileStorageService, IWebHostEnvironment env)
    {
        _mediaService = mediaService;
        _siteService = siteService;
        _fileStorageService = fileStorageService;
        _env = env;
    }

    [HttpGet]
    [EndpointSummary("List media files")]
    [EndpointDescription("Returns a paginated list of media files for the current site, including images, PDFs, and videos.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var media = await _mediaService.GetAllBySiteAsync(siteId, page: page, pageSize: pageSize);
        var total = await _mediaService.GetTotalCountAsync(siteId);

        return Ok(new
        {
            data = media,
            meta = new { page, pageSize, totalCount = total }
        });
    }

    [HttpPost("upload")]
    [EndpointSummary("Upload a media file")]
    [EndpointDescription("Uploads a single file to the media library. Accepts images (JPEG, PNG, GIF, WebP, SVG), PDFs, and MP4 videos up to 10MB.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Upload([FromServices] IMediaService mediaService, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = new { code = "NO_FILE", message = "No file provided." } });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { error = new { code = "FILE_TOO_LARGE", message = "File exceeds 10MB limit." } });

        if (!AllowedMimeTypes.Contains(file.ContentType))
            return BadRequest(new { error = new { code = "INVALID_TYPE", message = "File type not allowed." } });

        var userId = GetCurrentUserId();
        var siteId = HttpContext.GetCurrentSiteId();

        // Save file to disk
        var ext = Path.GetExtension(file.FileName);
        var filename = $"{Guid.NewGuid():N}{ext}";
        var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        Directory.CreateDirectory(uploadDir);

        var filePath = Path.Combine(uploadDir, filename);
        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Use UploadAsync to persist the record
        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var created = await _mediaService.UploadAsync(siteId, userId, file.FileName, file.ContentType, fileStream);

        return Ok(new
        {
            data = new
            {
                id = created.Id,
                url = $"/uploads/{filename}",
                filename = created.OriginalName,
                mimeType = created.MimeType,
                fileSize = created.FileSize
            }
        });
    }

    /// <summary>
    /// Bulk upload multiple files (up to 20).
    /// </summary>
    [HttpPost("bulk")]
    [EndpointSummary("Bulk upload media files")]
    [EndpointDescription("Uploads multiple files to the media library in a single request. Accepts up to 20 files, each up to 10MB.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> BulkUpload([FromServices] IMediaService mediaService, IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
            return BadRequest(new { error = new { code = "NO_FILES", message = "No files provided." } });
        if (files.Count > 20)
            return BadRequest(new { error = new { code = "TOO_MANY_FILES", message = "Maximum 20 files per upload." } });

        var userId = GetCurrentUserId();
        var siteId = HttpContext.GetCurrentSiteId();
        var results = new List<object>();

        foreach (var file in files)
        {
            if (file.Length == 0 || file.Length > 10 * 1024 * 1024) continue;
            if (!AllowedMimeTypes.Contains(file.ContentType)) continue;

            var ext = Path.GetExtension(file.FileName);
            var filename = $"{Guid.NewGuid():N}{ext}";
            var uploadDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            Directory.CreateDirectory(uploadDir);
            var filePath = Path.Combine(uploadDir, filename);

            await using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var created = await _mediaService.UploadAsync(siteId, userId, file.FileName, file.ContentType, fileStream);
            results.Add(new { id = created.Id, url = $"/uploads/{filename}", filename = created.OriginalName });
        }

        return Ok(new { data = results });
    }

    /// <summary>
    /// Update media metadata (alt text, caption, folder, tags, focal point).
    /// </summary>
    [HttpPatch("{id}")]
    [EndpointSummary("Update media metadata")]
    [EndpointDescription("Updates metadata for a media file including alt text, caption, folder, tags, and focal point coordinates.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateMetadata(string id, [FromBody] UpdateMediaRequest request)
    {
        if (!Guid.TryParse(id, out var mediaId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid media ID." } });

        var media = await _mediaService.GetByIdAsync(mediaId);
        if (media == null) return NotFound();

        if (request.AltText != null) media.AltText = request.AltText;
        if (request.Caption != null) media.Caption = request.Caption;
        if (request.Folder != null) media.Folder = request.Folder;
        if (request.Tags != null) media.Tags = request.Tags;
        if (request.FocalPointX.HasValue) media.FocalPointX = request.FocalPointX;
        if (request.FocalPointY.HasValue) media.FocalPointY = request.FocalPointY;

        await _mediaService.UpdateAsync(media);
        return Ok(new { data = media });
    }

    /// <summary>
    /// List distinct folders.
    /// </summary>
    [HttpGet("folders")]
    [EndpointSummary("List media folders")]
    [EndpointDescription("Returns a distinct sorted list of folder names used to organize media files for the current site.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> ListFolders()
    {
        var siteId = HttpContext.GetCurrentSiteId();
        var media = await _mediaService.GetAllBySiteAsync(siteId, page: 1, pageSize: 10000);
        var folders = media.Where(m => !string.IsNullOrEmpty(m.Folder)).Select(m => m.Folder).Distinct().OrderBy(f => f);
        return Ok(new { data = folders });
    }

    /// <summary>
    /// Move files to a folder.
    /// </summary>
    [HttpPost("move")]
    [EndpointSummary("Move files to a folder")]
    [EndpointDescription("Moves one or more media files into the specified folder. Pass a null or empty folder to move files to the root.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> MoveToFolder([FromBody] MoveMediaRequest request)
    {
        if (request.Ids == null || request.Ids.Length == 0)
            return BadRequest(new { error = new { code = "NO_IDS", message = "No file IDs provided." } });

        foreach (var id in request.Ids)
        {
            var media = await _mediaService.GetByIdAsync(id);
            if (media != null)
            {
                media.Folder = request.Folder;
                await _mediaService.UpdateAsync(media);
            }
        }
        return Ok(new { data = new { moved = request.Ids.Length } });
    }

    /// <summary>
    /// Bulk delete files.
    /// </summary>
    [HttpDelete("bulk")]
    [EndpointSummary("Bulk delete media files")]
    [EndpointDescription("Permanently deletes multiple media files by their IDs in a single request.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> BulkDelete([FromBody] BulkDeleteRequest request)
    {
        if (request.Ids == null || request.Ids.Length == 0)
            return BadRequest(new { error = new { code = "NO_IDS", message = "No file IDs provided." } });

        foreach (var id in request.Ids)
            await _mediaService.DeleteAsync(id);

        return Ok(new { data = new { deleted = request.Ids.Length } });
    }

    [HttpDelete("{id}")]
    [EndpointSummary("Delete a media file")]
    [EndpointDescription("Permanently deletes a single media file by its ID from the media library.")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        if (!Guid.TryParse(id, out var mediaId))
            return BadRequest(new { error = new { code = "INVALID_ID", message = "Invalid media ID." } });

        var media = await _mediaService.GetByIdAsync(mediaId);
        if (media == null)
            return NotFound();

        await _mediaService.DeleteAsync(mediaId);
        return NoContent();
    }

    /// <summary>
    /// Crop (and optionally resize) an image.
    /// </summary>
    [HttpPost("{id}/crop")]
    [EndpointSummary("Crop an image")]
    [EndpointDescription("Crops an image to the specified rectangle coordinates and optionally resizes the result. Overwrites the original file and updates dimensions.")]
    [ProducesResponseType(200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> CropImage(Guid id, [FromBody] CropRequest request)
    {
        var media = await _mediaService.GetByIdAsync(id);
        if (media == null)
            return NotFound();

        if (!media.MimeType.StartsWith("image/") || media.MimeType == "image/svg+xml")
            return BadRequest(new { error = new { code = "NOT_RASTER_IMAGE", message = "Only raster images can be cropped." } });

        if (request.Width <= 0 || request.Height <= 0)
            return BadRequest(new { error = new { code = "INVALID_DIMENSIONS", message = "Crop width and height must be positive." } });

        try
        {
            using var sourceStream = await _fileStorageService.ReadAsync(media.StoragePath);
            using var image = await Image.LoadAsync(sourceStream);

            // Validate crop bounds
            if (request.X < 0 || request.Y < 0 ||
                request.X + request.Width > image.Width ||
                request.Y + request.Height > image.Height)
            {
                return BadRequest(new { error = new { code = "OUT_OF_BOUNDS", message = "Crop rectangle exceeds image dimensions." } });
            }

            image.Mutate(ctx =>
            {
                ctx.Crop(new Rectangle(request.X, request.Y, request.Width, request.Height));

                if (request.OutputWidth.HasValue && request.OutputHeight.HasValue &&
                    request.OutputWidth.Value > 0 && request.OutputHeight.Value > 0)
                {
                    ctx.Resize(request.OutputWidth.Value, request.OutputHeight.Value);
                }
            });

            // Save back to storage
            using var outputStream = new MemoryStream();
            await image.SaveAsync(outputStream, image.Metadata.DecodedImageFormat!);
            outputStream.Position = 0;

            await _fileStorageService.UploadAsync(outputStream, media.StoragePath);

            // Update media record dimensions
            media.Width = image.Width;
            media.Height = image.Height;
            media.FileSize = outputStream.Length;
            await _mediaService.UpdateAsync(media);

            return Ok(new { data = media });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = new { code = "CROP_FAILED", message = ex.Message } });
        }
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirst("app_user_id")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }
}

public class CropRequest
{
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int? OutputWidth { get; set; }
    public int? OutputHeight { get; set; }
}

public class UpdateMediaRequest
{
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string? Folder { get; set; }
    public string[]? Tags { get; set; }
    public double? FocalPointX { get; set; }
    public double? FocalPointY { get; set; }
}

public class MoveMediaRequest
{
    public Guid[] Ids { get; set; } = [];
    public string? Folder { get; set; }
}

public class BulkDeleteRequest
{
    public Guid[] Ids { get; set; } = [];
}
