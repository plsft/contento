using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// A content schema — defines the JSON structure and prompt templates for AI-generated pages
/// </summary>
[Table("content_schemas")]
[TableOrder(30)]
public class ContentSchema
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("slug", MaxLength = 200)]
    [Index("ux_content_schemas_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("schema_json", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string SchemaJson { get; set; } = "{}";

    [Column("prompt_template", TypeName = "text")]
    public string PromptTemplate { get; set; } = string.Empty;

    [Column("user_prompt_template", TypeName = "text")]
    public string UserPromptTemplate { get; set; } = string.Empty;

    [Column("renderer_slug", MaxLength = 100)]
    public string RendererSlug { get; set; } = string.Empty;

    [Column("title_pattern", MaxLength = 500)]
    public string TitlePattern { get; set; } = string.Empty;

    [Column("meta_desc_pattern", MaxLength = 500)]
    public string? MetaDescPattern { get; set; }

    [Column("is_system", TypeName = "boolean")]
    [DefaultValue(true)]
    public bool IsSystem { get; set; } = true;

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
