using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Hierarchical content category
/// </summary>
[Table("categories")]
[TableOrder(4)]
public class Category
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ux_categories_site_slug", IsUnique = true)]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 200)]
    [Index("ux_categories_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("parent_id")]
    [ForeignKey("categories", ReferencedColumn = "id")]
    public Guid? ParentId { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
