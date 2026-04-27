using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Daily search analytics for a pSEO page — clicks, impressions, CTR, position
/// </summary>
[Table("pseo_analytics")]
[TableOrder(34)]
public class PseoAnalytics
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("page_id")]
    [ForeignKey("pseo_pages", ReferencedColumn = "id")]
    [Index("ix_pseo_analytics_page_id")]
    [Index("ux_pseo_analytics_page_date", IsUnique = true)]
    public Guid PageId { get; set; }

    [Column("date")]
    [Index("ix_pseo_analytics_date")]
    [Index("ux_pseo_analytics_page_date", IsUnique = true)]
    public DateTime Date { get; set; }

    [Column("clicks")]
    [DefaultValue(0)]
    public int Clicks { get; set; }

    [Column("impressions")]
    [DefaultValue(0)]
    public int Impressions { get; set; }

    [Column("ctr")]
    [DefaultValue(0)]
    public decimal Ctr { get; set; }

    [Column("position")]
    [DefaultValue(0)]
    public decimal Position { get; set; }

    [Column("is_indexed", TypeName = "boolean")]
    [DefaultValue(false)]
    public bool IsIndexed { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
