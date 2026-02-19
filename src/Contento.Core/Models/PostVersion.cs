using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Full revision history for posts
/// </summary>
[Table("post_versions")]
[TableOrder(6)]
public class PostVersion
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    [Index("ix_post_versions_post_id")]
    public Guid PostId { get; set; }

    [Column("version")]
    public int Version { get; set; }

    [Column("title", MaxLength = 900)]
    public string? Title { get; set; }

    [Column("body_markdown", TypeName = "text")]
    public string BodyMarkdown { get; set; } = string.Empty;

    [Column("body_html", TypeName = "text")]
    public string? BodyHtml { get; set; }

    [Column("change_summary", TypeName = "text")]
    public string? ChangeSummary { get; set; }

    [Column("changed_by")]
    [ForeignKey("users", ReferencedColumn = "id")]
    public Guid ChangedBy { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
