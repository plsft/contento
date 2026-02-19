using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Custom post type definition (e.g. Blog Post, Page, Product)
/// </summary>
[Table("post_types")]
[TableOrder(18)]
public class PostType
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_post_types_site_id")]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 100)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 100)]
    [Index("ux_post_types_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("icon", MaxLength = 50)]
    public string? Icon { get; set; }

    [Column("fields", TypeName = "jsonb")]
    [DefaultValue("'[]'", IsRawSql = true)]
    public string Fields { get; set; } = "[]";

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("is_system")]
    [DefaultValue(false)]
    public bool IsSystem { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
