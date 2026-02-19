using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Execution history for a scheduled task
/// </summary>
[Table("scheduled_task_logs")]
[TableOrder(20)]
public class ScheduledTaskLog
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("task_id")]
    [ForeignKey("scheduled_tasks", ReferencedColumn = "id")]
    [Index("ix_scheduled_task_logs_task_id")]
    public Guid TaskId { get; set; }

    [Column("started_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("status", MaxLength = 20)]
    public string Status { get; set; } = string.Empty;

    [Column("message", TypeName = "text")]
    public string? Message { get; set; }

    [Column("duration_ms")]
    [DefaultValue(0)]
    public int DurationMs { get; set; }
}
