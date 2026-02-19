using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A named navigation menu assigned to a location (header, footer, etc.)
/// </summary>
[Table("menus")]
[TableOrder(23)]
public class Menu
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ux_menus_site_slug", IsUnique = true)]
    [Index("ix_menus_site_location")]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 200)]
    [Index("ux_menus_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("location", MaxLength = 50)]
    [Index("ix_menus_site_location")]
    public string Location { get; set; } = string.Empty;

    [Column("is_active")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
