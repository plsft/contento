using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Visual theme for public site rendering
/// </summary>
[Table("themes")]
[TableOrder(3)]
public class Theme
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name", MaxLength = 400)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 200)]
    [Index("ux_themes_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("version", MaxLength = 100)]
    public string? Version { get; set; }

    [Column("author", MaxLength = 200)]
    public string? Author { get; set; }

    [Column("css_variables", TypeName = "jsonb")]
    public string? CssVariables { get; set; }

    [Column("base_layout_id")]
    [ForeignKey("layouts", ReferencedColumn = "id")]
    public Guid? BaseLayoutId { get; set; }

    [Column("thumbnail_url", MaxLength = 500)]
    public string? ThumbnailUrl { get; set; }

    [Column("is_active")]
    [DefaultValue(false)]
    public bool IsActive { get; set; }

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
