using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Represents a Contento site instance
/// </summary>
[Table("sites")]
[TableOrder(1)]
public class Site
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 200)]
    [Index("ux_sites_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("domain", MaxLength = 255)]
    public string? Domain { get; set; }

    [Column("tagline", MaxLength = 1024)]
    public string? Tagline { get; set; }

    [Column("locale", MaxLength = 16)]
    [DefaultValue("'en-US'", IsRawSql = true)]
    public string Locale { get; set; } = "en-US";

    [Column("timezone", MaxLength = 50)]
    [DefaultValue("'UTC'", IsRawSql = true)]
    public string Timezone { get; set; } = "UTC";

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("theme_id")]
    [ForeignKey("themes", ReferencedColumn = "id")]
    public Guid? ThemeId { get; set; }

    [Column("is_primary", TypeName = "boolean")]
    [DefaultValue(false)]
    public bool IsPrimary { get; set; }

    [Column("created_by")]
    [ForeignKey("users", ReferencedColumn = "id")]
    public Guid? CreatedBy { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
