using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Threaded comment on a post
/// </summary>
[Table("comments")]
[TableOrder(7)]
public class Comment
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("post_id")]
    [ForeignKey("posts", ReferencedColumn = "id")]
    [Index("ix_comments_post_status")]
    public Guid PostId { get; set; }

    [Column("parent_id")]
    [ForeignKey("comments", ReferencedColumn = "id")]
    [Index("ix_comments_parent_id")]
    public Guid? ParentId { get; set; }

    [Column("author_name", MaxLength = 200)]
    public string AuthorName { get; set; } = string.Empty;

    [Column("author_email", MaxLength = 300)]
    public string? AuthorEmail { get; set; }

    [Column("author_url", MaxLength = 500)]
    public string? AuthorUrl { get; set; }

    [Column("author_user_id")]
    [ForeignKey("users", ReferencedColumn = "id")]
    public Guid? AuthorUserId { get; set; }

    [Column("body_markdown", TypeName = "text")]
    public string BodyMarkdown { get; set; } = string.Empty;

    [Column("body_html", TypeName = "text")]
    public string? BodyHtml { get; set; }

    [Column("status", MaxLength = 20)]
    [DefaultValue("'pending'", IsRawSql = true)]
    [Index("ix_comments_post_status")]
    public string Status { get; set; } = "pending";

    [Column("ip_address", MaxLength = 45)]
    public string? IpAddress { get; set; }

    [Column("user_agent", MaxLength = 500)]
    public string? UserAgent { get; set; }

    [Column("likes_count")]
    [DefaultValue(0)]
    public int LikesCount { get; set; }

    [Column("depth")]
    [DefaultValue(0)]
    public int Depth { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    [Index("ix_comments_post_status")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
