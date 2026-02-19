using Noundry.Tuxedo.Contrib;
using Noundry.Tuxedo.Bowtie.Attributes;

namespace Contento.Core.Models;

/// <summary>
/// Application user with role-based permissions
/// </summary>
[Table("users")]
[TableOrder(2)]
public class User
{
    [ExplicitKey]
    [Column("id")]
    [DefaultValue("gen_random_uuid()", IsRawSql = true)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Column("email", MaxLength = 255)]
    [Index("ux_users_email", IsUnique = true)]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash", MaxLength = 255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("display_name", MaxLength = 300)]
    public string DisplayName { get; set; } = string.Empty;

    [Column("bio", TypeName = "text")]
    public string? Bio { get; set; }

    [Column("avatar_url", MaxLength = 500)]
    public string? AvatarUrl { get; set; }

    [Column("role", MaxLength = 50)]
    [DefaultValue("'author'", IsRawSql = true)]
    public string Role { get; set; } = "author";

    [Column("preferences", TypeName = "jsonb")]
    [DefaultValue("'{}'", IsRawSql = true)]
    public string Preferences { get; set; } = "{}";

    [Column("is_active")]
    [DefaultValue(true)]
    public bool IsActive { get; set; } = true;

    [Column("stripe_customer_id", MaxLength = 255)]
    public string? StripeCustomerId { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    [DefaultValue("CURRENT_TIMESTAMP", IsRawSql = true)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [Column("password_reset_token", MaxLength = 255)]
    public string? PasswordResetToken { get; set; }

    [Column("password_reset_expires_at")]
    public DateTime? PasswordResetExpiresAt { get; set; }

    [Column("email_verified")]
    [DefaultValue(false)]
    public bool EmailVerified { get; set; }

    [Column("email_verification_token", MaxLength = 255)]
    public string? EmailVerificationToken { get; set; }
}
