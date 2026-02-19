using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Newsletter subscriber and/or Stripe member
/// </summary>
[Table("subscribers")]
[TableOrder(14)]
public class Subscriber
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_subscribers_site_id")]
    public Guid SiteId { get; set; }

    [Column("email", MaxLength = 255)]
    [Index("ux_subscribers_site_email", IsUnique = true)]
    public string Email { get; set; } = string.Empty;

    [Column("display_name", MaxLength = 300)]
    public string? DisplayName { get; set; }

    [Column("status", MaxLength = 20)]
    [DefaultValue("'active'", IsRawSql = true)]
    public string Status { get; set; } = "active";

    [Column("stripe_customer_id", MaxLength = 255)]
    public string? StripeCustomerId { get; set; }

    [Column("membership_tier", MaxLength = 50)]
    [DefaultValue("'free'", IsRawSql = true)]
    public string MembershipTier { get; set; } = "free";

    [Column("membership_expires_at")]
    public DateTime? MembershipExpiresAt { get; set; }

    [Column("membership_plan_id")]
    [ForeignKey("membership_plans", ReferencedColumn = "id")]
    public Guid? MembershipPlanId { get; set; }

    [Column("trial_ends_at")]
    public DateTime? TrialEndsAt { get; set; }

    [Column("stripe_subscription_id", MaxLength = 255)]
    public string? StripeSubscriptionId { get; set; }

    [Column("payment_failure_count")]
    [DefaultValue(0)]
    public int PaymentFailureCount { get; set; }

    [Column("unsubscribe_token", MaxLength = 100)]
    [Index("ux_subscribers_unsubscribe_token", IsUnique = true)]
    public string UnsubscribeToken { get; set; } = string.Empty;

    [Column("subscribed_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
