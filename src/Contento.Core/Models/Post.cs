using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A content post — the core entity of Contento
/// </summary>
[Table("posts")]
[TableOrder(5)]
public class Post
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_posts_site_status")]
    public Guid SiteId { get; set; }

    [Column("title", MaxLength = 500)]
    public string Title { get; set; } = string.Empty;

    [Column("slug", MaxLength = 500)]
    [Index("ux_posts_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("subtitle", MaxLength = 500)]
    public string? Subtitle { get; set; }

    [Column("excerpt", TypeName = "text")]
    public string? Excerpt { get; set; }

    [Column("body_markdown", TypeName = "text")]
    public string BodyMarkdown { get; set; } = string.Empty;

    [Column("body_html", TypeName = "text")]
    public string? BodyHtml { get; set; }

    [Column("cover_image_url", MaxLength = 750)]
    public string? CoverImageUrl { get; set; }

    [Column("author_id")]
    [ForeignKey("users", ReferencedColumn = "id")]
    [Index("ix_posts_author_id")]
    public Guid AuthorId { get; set; }

    [Column("status", MaxLength = 20)]
    [DefaultValue("'draft'", IsRawSql = true)]
    [Index("ix_posts_site_status")]
    public string Status { get; set; } = "draft";

    [Column("visibility", MaxLength = 20)]
    [DefaultValue("'public'", IsRawSql = true)]
    public string Visibility { get; set; } = "public";

    [Column("password_hash", MaxLength = 200)]
    public string? PasswordHash { get; set; }

    [Column("published_at")]
    [Index("ix_posts_published_at")]
    public DateTime? PublishedAt { get; set; }

    [Column("scheduled_at")]
    public DateTime? ScheduledAt { get; set; }

    [Column("featured")]
    [DefaultValue(false)]
    public bool Featured { get; set; }

    [Column("reading_time_minutes")]
    public int? ReadingTimeMinutes { get; set; }

    [Column("word_count")]
    public int? WordCount { get; set; }

    [Column("meta_title", MaxLength = 400)]
    public string? MetaTitle { get; set; }

    [Column("meta_description", MaxLength = 1000)]
    public string? MetaDescription { get; set; }

    [Column("og_image_url", MaxLength = 1000)]
    public string? OgImageUrl { get; set; }

    [Column("canonical_url", MaxLength = 900)]
    public string? CanonicalUrl { get; set; }

    [Column("schema_markup", TypeName = "jsonb")]
    public string? SchemaMarkup { get; set; }

    [Column("tags", TypeName = "text[]")]
    public string[]? Tags { get; set; }

    [Column("category_id")]
    [ForeignKey("categories", ReferencedColumn = "id")]
    [Index("ix_posts_category_id")]
    public Guid? CategoryId { get; set; }

    [Column("layout_id")]
    [ForeignKey("layouts", ReferencedColumn = "id")]
    public Guid? LayoutId { get; set; }

    [Column("custom_css", TypeName = "text")]
    public string? CustomCss { get; set; }

    [Column("custom_js", TypeName = "text")]
    public string? CustomJs { get; set; }

    [Column("post_type_id")]
    [ForeignKey("post_types", ReferencedColumn = "id")]
    public Guid? PostTypeId { get; set; }

    [Column("custom_fields", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string CustomFields { get; set; } = "{}";

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("version")]
    [DefaultValue(1)]
    public int Version { get; set; } = 1;

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
