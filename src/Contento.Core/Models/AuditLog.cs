using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Audit trail for all state-changing actions
/// </summary>
[Table("audit_log")]
[TableOrder(12)]
public class AuditLog
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_audit_log_site_id")]
    public Guid? SiteId { get; set; }

    [Column("user_id")]
    [ForeignKey("users", ReferencedColumn = "id")]
    [Index("ix_audit_log_user_id")]
    public Guid? UserId { get; set; }

    [Column("action", MaxLength = 100)]
    [Index("ix_audit_log_action")]
    public string Action { get; set; } = string.Empty;

    [Column("entity_type", MaxLength = 50)]
    [Index("ix_audit_log_entity")]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    [Index("ix_audit_log_entity")]
    public Guid? EntityId { get; set; }

    [Column("details", TypeName = "jsonb")]
    public string? Details { get; set; }

    [Column("ip_address", MaxLength = 45)]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    [Index("ix_audit_log_created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
