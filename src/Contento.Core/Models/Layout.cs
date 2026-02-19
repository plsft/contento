using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Page layout template defining region arrangement
/// </summary>
[Table("layouts")]
[TableOrder(3)]
public class Layout
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ux_layouts_site_slug", IsUnique = true)]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 100)]
    [Index("ux_layouts_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("is_default")]
    [DefaultValue(false)]
    public bool IsDefault { get; set; }

    [Column("structure", TypeName = "jsonb")]
    public string Structure { get; set; } = "{}";

    [Column("head_content", TypeName = "text")]
    public string? HeadContent { get; set; }

    [Column("custom_css", TypeName = "text")]
    public string? CustomCss { get; set; }

    [Column("custom_js", TypeName = "text")]
    public string? CustomJs { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
