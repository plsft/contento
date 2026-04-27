using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A pSEO collection — groups pages under a project with shared schema and publish settings
/// </summary>
[Table("pseo_collections")]
[TableOrder(31)]
public class PseoCollection
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("project_id")]
    [ForeignKey("pseo_projects", ReferencedColumn = "id")]
    [Index("ix_pseo_collections_project_id")]
    public Guid ProjectId { get; set; }

    [Column("schema_id")]
    [ForeignKey("content_schemas", ReferencedColumn = "id")]
    public Guid SchemaId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("url_pattern", MaxLength = 500)]
    public string UrlPattern { get; set; } = string.Empty;

    [Column("title_template", MaxLength = 500)]
    public string TitleTemplate { get; set; } = string.Empty;

    [Column("meta_desc_template", MaxLength = 500)]
    public string? MetaDescTemplate { get; set; }

    [Column("publish_schedule", MaxLength = 20)]
    [DefaultValue("'manual'", IsRawSql = true)]
    public string PublishSchedule { get; set; } = "manual";

    [Column("batch_size")]
    [DefaultValue(50)]
    public int BatchSize { get; set; } = 50;

    [Column("status", MaxLength = 20)]
    [DefaultValue("'draft'", IsRawSql = true)]
    public string Status { get; set; } = "draft";

    [Column("page_count")]
    [DefaultValue(0)]
    public int PageCount { get; set; }

    [Column("generated_count")]
    [DefaultValue(0)]
    public int GeneratedCount { get; set; }

    [Column("published_count")]
    [DefaultValue(0)]
    public int PublishedCount { get; set; }

    [Column("failed_count")]
    [DefaultValue(0)]
    public int FailedCount { get; set; }

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
