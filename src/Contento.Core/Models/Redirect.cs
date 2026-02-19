using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Represents a URL redirect rule (301/302) for a site.
/// Used to preserve SEO when slugs change or pages move.
/// </summary>
[Table("redirects")]
[TableOrder(27)]
public class Redirect
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_redirects_site_from", IsUnique = false)]
    public Guid SiteId { get; set; }

    [Column("from_path", MaxLength = 1000)]
    [Index("ux_redirects_site_from", IsUnique = true)]
    public string FromPath { get; set; } = string.Empty;

    [Column("to_path", MaxLength = 1000)]
    public string ToPath { get; set; } = string.Empty;

    [Column("status_code")]
    [DefaultValue(301)]
    public int StatusCode { get; set; } = 301;

    [Column("is_active")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;

    [Column("hit_count")]
    [DefaultValue(0)]
    public int HitCount { get; set; }

    [Column("last_hit_at")]
    public DateTime? LastHitAt { get; set; }

    [Column("notes", TypeName = "text")]
    public string? Notes { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
