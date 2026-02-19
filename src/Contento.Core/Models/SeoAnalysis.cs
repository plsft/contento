using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Per-post SEO analysis scores and issues
/// </summary>
[Table("seo_analyses")]
[TableOrder(22)]
public class SeoAnalysis
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    [Index("ux_seo_analyses_post_id", IsUnique = true)]
    public Guid PostId { get; set; }

    [Column("overall_score")]
    [DefaultValue(0)]
    public int OverallScore { get; set; }

    [Column("issues", TypeName = "jsonb")]
    [DefaultValue("'[]'", IsRawSql = true)]
    public string Issues { get; set; } = "[]";

    [Column("focus_keyword", MaxLength = 200)]
    public string? FocusKeyword { get; set; }

    [Column("keyword_density", TypeName = "numeric(5,2)")]
    public decimal? KeywordDensity { get; set; }

    [Column("readability_score")]
    public int? ReadabilityScore { get; set; }

    [Column("last_analyzed_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime LastAnalyzedAt { get; set; } = DateTime.UtcNow;
}
