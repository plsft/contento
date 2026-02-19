using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Idempotency log for Stripe webhook events
/// </summary>
[Table("stripe_events")]
[TableOrder(16)]
public class StripeEvent
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("event_id", MaxLength = 255)]
    [Index("ux_stripe_events_event_id", IsUnique = true)]
    public string EventId { get; set; } = string.Empty;

    [Column("event_type", MaxLength = 100)]
    public string EventType { get; set; } = string.Empty;

    [Column("processed_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
