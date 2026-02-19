using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Media library asset (images, files, etc.)
/// </summary>
[Table("media")]
[TableOrder(9)]
public class Media
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_media_site_id")]
    public Guid SiteId { get; set; }

    [Column("filename", MaxLength = 500)]
    public string Filename { get; set; } = string.Empty;

    [Column("original_name", MaxLength = 500)]
    public string OriginalName { get; set; } = string.Empty;

    [Column("mime_type", MaxLength = 100)]
    public string MimeType { get; set; } = string.Empty;

    [Column("file_size")]
    public long FileSize { get; set; }

    [Column("width")]
    public int? Width { get; set; }

    [Column("height")]
    public int? Height { get; set; }

    [Column("alt_text", MaxLength = 500)]
    public string? AltText { get; set; }

    [Column("caption", TypeName = "text")]
    public string? Caption { get; set; }

    [Column("storage_path", MaxLength = 1000)]
    public string StoragePath { get; set; } = string.Empty;

    [Column("thumbnail_path", MaxLength = 1000)]
    public string? ThumbnailPath { get; set; }

    [Column("folder", MaxLength = 200)]
    public string? Folder { get; set; }

    [Column("tags", TypeName = "text[]")]
    public string[]? Tags { get; set; }

    [Column("focal_point_x")]
    public double? FocalPointX { get; set; }

    [Column("focal_point_y")]
    public double? FocalPointY { get; set; }

    [Column("uploaded_by")]
    [ForeignKey("users", ReferencedColumn = "id")]
    public Guid UploadedBy { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
