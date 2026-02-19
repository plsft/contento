using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Cron-like scheduled task definition
/// </summary>
[Table("scheduled_tasks")]
[TableOrder(19)]
public class ScheduledTask
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_scheduled_tasks_site_id")]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("task_type", MaxLength = 100)]
    public string TaskType { get; set; } = string.Empty;

    [Column("cron_expression", MaxLength = 100)]
    public string CronExpression { get; set; } = string.Empty;

    [Column("is_enabled")]
    [DefaultValue(true)]
    public bool IsEnabled { get; set; } = true;

    [Column("last_run_at")]
    public DateTime? LastRunAt { get; set; }

    [Column("next_run_at")]
    public DateTime? NextRunAt { get; set; }

    [Column("last_result", MaxLength = 20)]
    public string? LastResult { get; set; }

    [Column("last_error", TypeName = "text")]
    public string? LastError { get; set; }

    [Column("settings", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Settings { get; set; } = "{}";

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
