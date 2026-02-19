using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Component placed within a layout region
/// </summary>
[Table("layout_components")]
[TableOrder(8)]
public class LayoutComponent
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("layout_id")]
    [ForeignKey("layouts", ReferencedColumn = "id")]
    [Index("ix_layout_components_layout_id")]
    public Guid LayoutId { get; set; }

    [Column("region", MaxLength = 50)]
    public string Region { get; set; } = string.Empty;

    [Column("content_type", MaxLength = 30)]
    public string ContentType { get; set; } = string.Empty;

    [Column("content", TypeName = "text")]
    public string? Content { get; set; }

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("is_visible")]
    [DefaultValue(true)]
    public bool IsVisible { get; set; } = true;

    [Column("css_classes", TypeName = "text")]
    public string? CssClasses { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
