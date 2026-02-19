using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A single link within a navigation menu
/// </summary>
[Table("menu_items")]
[TableOrder(24)]
public class MenuItem
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("menu_id")]
    [ForeignKey("menus", ReferencedColumn = "id")]
    [Index("ix_menu_items_menu_sort")]
    public Guid MenuId { get; set; }

    [Column("parent_id")]
    [ForeignKey("menu_items", ReferencedColumn = "id")]
    public Guid? ParentId { get; set; }

    [Column("label", MaxLength = 200)]
    public string Label { get; set; } = string.Empty;

    [Column("url", MaxLength = 500)]
    public string? Url { get; set; }

    [Column("link_type", MaxLength = 50)]
    [DefaultValue("'custom'", IsRawSql = true)]
    public string LinkType { get; set; } = "custom";

    [Column("link_id")]
    public Guid? LinkId { get; set; }

    [Column("target", MaxLength = 20)]
    [DefaultValue("'_self'", IsRawSql = true)]
    public string Target { get; set; } = "_self";

    [Column("css_class", MaxLength = 200)]
    public string? CssClass { get; set; }

    [Column("sort_order")]
    [DefaultValue(0)]
    [Index("ix_menu_items_menu_sort")]
    public int SortOrder { get; set; }

    [Column("is_visible")]
    [DefaultValue(true)]
    public bool IsVisible { get; set; } = true;

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
