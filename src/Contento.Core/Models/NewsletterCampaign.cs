using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A sent newsletter campaign record
/// </summary>
[Table("newsletter_campaigns")]
[TableOrder(15)]
public class NewsletterCampaign
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_newsletter_campaigns_site_id")]
    public Guid SiteId { get; set; }

    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    public Guid? PostId { get; set; }

    [Column("subject", MaxLength = 500)]
    public string Subject { get; set; } = string.Empty;

    [Column("body_html", TypeName = "text")]
    public string BodyHtml { get; set; } = string.Empty;

    [Column("status", MaxLength = 20)]
    [DefaultValue("'draft'", IsRawSql = true)]
    public string Status { get; set; } = "draft";

    [Column("sent_count")]
    [DefaultValue(0)]
    public int SentCount { get; set; }

    [Column("failed_count")]
    [DefaultValue(0)]
    public int FailedCount { get; set; }

    [Column("sent_at")]
    public DateTime? SentAt { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
