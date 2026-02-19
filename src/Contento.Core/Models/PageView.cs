using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Individual page view tracking record
/// </summary>
[Table("page_views")]
[TableOrder(11)]
public class PageView
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    [Index("ix_page_views_post_date")]
    public Guid? PostId { get; set; }

    [Column("session_id", MaxLength = 100)]
    public string? SessionId { get; set; }

    [Column("ip_hash", MaxLength = 64)]
    public string? IpHash { get; set; }

    [Column("country_code", MaxLength = 2)]
    public string? CountryCode { get; set; }

    [Column("referrer", MaxLength = 1000)]
    public string? Referrer { get; set; }

    [Column("user_agent", TypeName = "text")]
    public string? UserAgent { get; set; }

    [Column("device_type", MaxLength = 20)]
    public string? DeviceType { get; set; }

    [Column("utm_source", MaxLength = 500)]
    public string? UtmSource { get; set; }

    [Column("utm_medium", MaxLength = 500)]
    public string? UtmMedium { get; set; }

    [Column("utm_campaign", MaxLength = 500)]
    public string? UtmCampaign { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    [Index("ix_page_views_post_date")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
