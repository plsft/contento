using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Membership tier/plan for paid subscriptions
/// </summary>
[Table("membership_plans")]
[TableOrder(21)]
public class MembershipPlan
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("site_id")]
    [ForeignKey("sites", ReferencedColumn = "id")]
    [Index("ix_membership_plans_site_id")]
    public Guid SiteId { get; set; }

    [Column("name", MaxLength = 200)]
    public string Name { get; set; } = string.Empty;

    [Column("slug", MaxLength = 100)]
    [Index("ux_membership_plans_site_slug", IsUnique = true)]
    public string Slug { get; set; } = string.Empty;

    [Column("description", TypeName = "text")]
    public string? Description { get; set; }

    [Column("stripe_price_id", MaxLength = 255)]
    public string? StripePriceId { get; set; }

    [Column("price", TypeName = "numeric(10,2)")]
    [DefaultValue(0)]
    public decimal Price { get; set; }

    [Column("currency", MaxLength = 3)]
    [DefaultValue("'usd'", IsRawSql = true)]
    public string Currency { get; set; } = "usd";

    [Column("billing_interval", MaxLength = 20)]
    [DefaultValue("'monthly'", IsRawSql = true)]
    public string BillingInterval { get; set; } = "monthly";

    [Column("features", TypeName = "jsonb")]
    [DefaultValue("'[]'", IsRawSql = true)]
    public string Features { get; set; } = "[]";

    [Column("access_level")]
    [DefaultValue(1)]
    public int AccessLevel { get; set; } = 1;

    [Column("trial_days")]
    [DefaultValue(0)]
    public int TrialDays { get; set; }

    [Column("is_active")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;

    [Column("sort_order")]
    [DefaultValue(0)]
    public int SortOrder { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
