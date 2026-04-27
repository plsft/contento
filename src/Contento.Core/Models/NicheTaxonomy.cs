using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A niche taxonomy — defines audience, pain points, and content strategy for a vertical
/// </summary>
[Table("niche_taxonomies")]
[TableOrder(29)]
public class NicheTaxonomy
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("slug", MaxLength = 200)]
    [Index("ux_niche_taxonomies_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("category", MaxLength = 200)]
    public string Category { get; set; } = string.Empty;

    [Column("context", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Context { get; set; } = "{}";

    [Column("is_system", TypeName = "boolean")]
    [DefaultValue(true)]
    public bool IsSystem { get; set; } = true;

    [Column("project_id")]
    [ForeignKey("pseo_projects", ReferencedColumn = "id")]
    public Guid? ProjectId { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
