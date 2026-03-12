using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Web.Middleware;
using MediaModel = Contento.Core.Models.Media;

namespace Contento.Web.Pages.Admin.Media;

public class IndexModel : PageModel
{
    private readonly IMediaService _mediaService;
    private readonly ISiteService _siteService;
    private readonly ILogger<IndexModel> _logger;
    private const int PageSize = 30;

    public IndexModel(IMediaService mediaService, ISiteService siteService, ILogger<IndexModel> logger)
    {
        _mediaService = mediaService;
        _siteService = siteService;
        _logger = logger;
    }

    public IEnumerable<MediaModel> MediaItems { get; set; } = [];
    public List<string> Folders { get; set; } = [];
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }

    public async Task OnGetAsync()
    {
        var siteId = HttpContext.GetCurrentSiteId();

        try
        {
            MediaItems = await _mediaService.GetAllBySiteAsync(siteId, page: 1, pageSize: PageSize);
            TotalCount = await _mediaService.GetTotalCountAsync(siteId);
            HasMore = TotalCount > PageSize;

            // Load distinct folders
            var allMedia = await _mediaService.GetAllBySiteAsync(siteId, page: 1, pageSize: 10000);
            Folders = allMedia
                .Where(m => !string.IsNullOrEmpty(m.Folder))
                .Select(m => m.Folder!)
                .Distinct()
                .OrderBy(f => f)
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load media items in {Page}", nameof(IndexModel));
        }
    }

    public string GetThumbnailUrl(MediaModel media)
    {
        if (!string.IsNullOrEmpty(media.ThumbnailPath))
            return _mediaService.GetThumbnailUrl(media.ThumbnailPath) ?? _mediaService.GetPublicUrl(media.StoragePath);
        return _mediaService.GetPublicUrl(media.StoragePath);
    }

    public string GetPublicUrl(MediaModel media)
    {
        return _mediaService.GetPublicUrl(media.StoragePath);
    }

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        return $"{bytes / (1024.0 * 1024.0):F1} MB";
    }
}
