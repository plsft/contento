using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A pSEO project — hosts generated pages on a subdomain
/// </summary>
[Table("pseo_projects")]
[TableOrder(28)]
public class PseoProject
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_pseo_projects_site_id")]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("root_domain", MaxLength = 255)]
    public string RootDomain { get; set; } = string.Empty;

    [Column("subdomain", MaxLength = 100)]
    public string Subdomain { get; set; } = string.Empty;

    [Column("fqdn", MaxLength = 255)]
    [Index("ux_pseo_projects_fqdn", IsUnique = true)]
    public string Fqdn { get; set; } = string.Empty;

    [Column("status", MaxLength = 30)]
    [DefaultValue("'pending_dns'", IsRawSql = true)]
    public string Status { get; set; } = "pending_dns";

    [Column("header_html", TypeName = "text")]
    public string? HeaderHtml { get; set; }

    [Column("footer_html", TypeName = "text")]
    public string? FooterHtml { get; set; }

    [Column("custom_css", TypeName = "text")]
    public string? CustomCss { get; set; }

    [Column("back_link_text", MaxLength = 200)]
    [DefaultValue("'Back to our site'", IsRawSql = true)]
    public string BackLinkText { get; set; } = "Back to our site";

    [Column("back_link_url", MaxLength = 500)]
    public string? BackLinkUrl { get; set; }

    [Column("cta_html", TypeName = "text")]
    public string? CtaHtml { get; set; }

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
