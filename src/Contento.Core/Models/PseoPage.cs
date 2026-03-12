using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A single pSEO page — AI-generated content validated against a content schema
/// </summary>
[Table("pseo_pages")]
[TableOrder(33)]
public class PseoPage
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("collection_id")]
    [ForeignKey("pseo_collections", ReferencedColumn = "id")]
    [Index("ix_pseo_pages_collection_id")]
    public Guid CollectionId { get; set; }

    [Column("project_id")]
    [ForeignKey("pseo_projects", ReferencedColumn = "id")]
    [Index("ux_pseo_pages_project_slug", IsUnique = true)]
    public Guid ProjectId { get; set; }

    [Column("niche_slug", MaxLength = 200)]
    public string NicheSlug { get; set; } = string.Empty;

    [Column("subtopic", MaxLength = 200)]
    public string Subtopic { get; set; } = string.Empty;

    [Column("slug", MaxLength = 500)]
    [Index("ix_pseo_pages_slug")]
    [Index("ux_pseo_pages_project_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("title", MaxLength = 500)]
    public string Title { get; set; } = string.Empty;

    [Column("meta_description", MaxLength = 1000)]
    public string? MetaDescription { get; set; }

    [Column("content_json", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string ContentJson { get; set; } = "{}";

    [Column("body_html", TypeName = "text")]
    public string? BodyHtml { get; set; }

    [Column("status", MaxLength = 20)]
    [DefaultValue("'pending'", IsRawSql = true)]
    public string Status { get; set; } = "pending";

    [Column("validation_errors", TypeName = "jsonb")]
    [DefaultValue("'[]'", IsRawSql = true)]
    public string ValidationErrors { get; set; } = "[]";

    [Column("published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    public Guid? PostId { get; set; }

    [Column("generation_duration_ms")]
    public int? GenerationDurationMs { get; set; }

    [Column("word_count")]
    [DefaultValue(0)]
    public int WordCount { get; set; }

    [Column("retry_count")]
    [DefaultValue(0)]
    public int RetryCount { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
