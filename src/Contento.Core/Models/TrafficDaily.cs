using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Aggregated daily traffic metrics
/// </summary>
[Table("traffic_daily")]
[TableOrder(13)]
public class TrafficDaily
{
    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    public Guid PostId { get; set; }

    [Column("date")]
    public DateTime Date { get; set; }

    [Column("views")]
    [DefaultValue(0)]
    public int Views { get; set; }

    [Column("unique_visitors")]
    [DefaultValue(0)]
    public int UniqueVisitors { get; set; }

    [Column("avg_time_on_page_seconds")]
    public int? AvgTimeOnPageSeconds { get; set; }

    [Column("bounce_rate")]
    public decimal? BounceRate { get; set; }

    [Column("top_referrers", TypeName = "jsonb")]
    public string? TopReferrers { get; set; }

    [Column("top_countries", TypeName = "jsonb")]
    public string? TopCountries { get; set; }
}
