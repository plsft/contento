using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Anti-spam verdict for a comment
/// </summary>
[Table("spam_scores")]
[TableOrder(17)]
public class SpamScore
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("comment_id")]
    [ForeignKey("comments", ReferencedColumn = "id")]
    [Index("ux_spam_scores_comment_id", IsUnique = true)]
    public Guid CommentId { get; set; }

    [Column("score", TypeName = "numeric(5,4)")]
    [DefaultValue(0)]
    public decimal Score { get; set; }

    [Column("reasons", TypeName = "jsonb")]
    [DefaultValue("'[]'", IsRawSql = true)]
    public string Reasons { get; set; } = "[]";

    [Column("is_spam")]
    [DefaultValue(false)]
    public bool IsSpam { get; set; }

    [Column("checked_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}
