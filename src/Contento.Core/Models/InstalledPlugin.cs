using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A plugin installed on a site
/// </summary>
[Table("installed_plugins")]
[TableOrder(10)]
public class InstalledPlugin
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ux_plugins_site_slug", IsUnique = true)]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 100)]
    [Index("ux_plugins_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("version", MaxLength = 20)]
    public string Version { get; set; } = string.Empty;

    [Column("author", MaxLength = 200)]
    public string? Author { get; set; }

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("entry_point", TypeName = "text")]
    public string EntryPoint { get; set; } = string.Empty;

    [Column("permissions", TypeName = "text[]")]
    public string[]? Permissions { get; set; }

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("is_enabled")]
    [DefaultValue(true)]
    public bool IsEnabled { get; set; } = true;

    [Column("installed_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime InstalledAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
