using System.Data;
using System.Security.Cryptography;
using Noundry.Guardian;
using Noundry.Tuxedo;
using Noundry.Tuxedo.Contrib;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;

namespace Contento.Services;

/// <summary>
/// Service for managing application users with full CRUD, BCrypt password
/// hashing, login validation, and role management.
/// </summary>
public class UserService : IUserService
{
    private readonly IDbConnection _db;
    private readonly ILogger<UserService> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="UserService"/>.
    /// </summary>
    /// <param name="db">The database connection.</param>
    /// <param name="logger">The logger.</param>
    public UserService(IDbConnection db, ILogger<UserService> logger)
    {
        _db = Guard.Against.Null(db);
        _logger = Guard.Against.Null(logger);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(Guid id)
    {
        Guard.Against.Default(id);
        return await _db.GetAsync<User>(id);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email)
    {
        Guard.Against.NullOrWhiteSpace(email);

        var results = await _db.QueryAsync<User>(
            "SELECT * FROM users WHERE email = @Email LIMIT 1",
            new { Email = email.ToLowerInvariant().Trim() });
        return results.FirstOrDefault();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 50)
    {
        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<User>(
            "SELECT * FROM users WHERE is_active = TRUE ORDER BY display_name LIMIT @Limit OFFSET @Offset",
            new { Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<IEnumerable<User>> GetByRoleAsync(string role, int page = 1, int pageSize = 50)
    {
        Guard.Against.NullOrWhiteSpace(role);

        var offset = (Math.Max(page, 1) - 1) * pageSize;
        return await _db.QueryAsync<User>(
            "SELECT * FROM users WHERE role = @Role AND is_active = TRUE ORDER BY display_name LIMIT @Limit OFFSET @Offset",
            new { Role = role, Limit = pageSize, Offset = offset });
    }

    /// <inheritdoc />
    public async Task<User> CreateAsync(User user, string password)
    {
        Guard.Against.Null(user);
        Guard.Against.NullOrWhiteSpace(user.Email);
        Guard.Against.NullOrWhiteSpace(user.DisplayName);
        Guard.Against.NullOrWhiteSpace(password);

        user.Id = Guid.NewGuid();
        user.Email = user.Email.ToLowerInvariant().Trim();
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.IsActive = true;
        user.CreatedAt = DateTime.UtcNow;

        await _db.InsertAsync(user);
        return user;
    }

    /// <inheritdoc />
    public async Task<User> UpdateAsync(User user)
    {
        Guard.Against.Null(user);
        Guard.Against.Default(user.Id);

        user.UpdatedAt = DateTime.UtcNow;
        await _db.UpdateAsync(user);
        return user;
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE users SET is_active = FALSE, updated_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<User?> ValidatePasswordAsync(string email, string password)
    {
        Guard.Against.NullOrWhiteSpace(email);
        Guard.Against.NullOrWhiteSpace(password);

        var user = await GetByEmailAsync(email);
        if (user == null || !user.IsActive)
            return null;

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return null;

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    /// <inheritdoc />
    public async Task<bool> UpdatePasswordAsync(Guid id, string currentPassword, string newPassword)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(currentPassword);
        Guard.Against.NullOrWhiteSpace(newPassword);

        var user = await _db.GetAsync<User>(id);
        if (user == null)
            return false;

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return false;

        // Hash and store new password
        var newHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash, updated_at = @Now WHERE id = @Id",
            new { Hash = newHash, Now = DateTime.UtcNow, Id = id });

        return true;
    }

    /// <inheritdoc />
    public async Task UpdateLastLoginAsync(Guid id)
    {
        Guard.Against.Default(id);

        await _db.ExecuteAsync(
            "UPDATE users SET last_login_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<int> GetTotalCountAsync(string? role = null)
    {
        if (!string.IsNullOrWhiteSpace(role))
        {
            return await _db.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM users WHERE role = @Role AND is_active = TRUE",
                new { Role = role });
        }

        return await _db.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM users WHERE is_active = TRUE",
            new { });
    }

    /// <inheritdoc />
    public async Task UpdateRoleAsync(Guid id, string role)
    {
        Guard.Against.Default(id);
        Guard.Against.NullOrWhiteSpace(role);

        await _db.ExecuteAsync(
            "UPDATE users SET role = @Role, updated_at = @Now WHERE id = @Id",
            new { Role = role, Now = DateTime.UtcNow, Id = id });
    }

    /// <inheritdoc />
    public async Task<string> RequestPasswordResetAsync(string email)
    {
        Guard.Against.NullOrWhiteSpace(email);

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTime.UtcNow.AddHours(1);

        var user = await GetByEmailAsync(email);
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return token;
        }

        await _db.ExecuteAsync(
            "UPDATE users SET password_reset_token = @Token, password_reset_expires_at = @ExpiresAt, updated_at = @Now WHERE id = @Id",
            new { Token = token, ExpiresAt = expiresAt, Now = DateTime.UtcNow, Id = user.Id });

        return token;
    }

    /// <inheritdoc />
    public async Task<bool> ResetPasswordAsync(string token, string newPassword)
    {
        Guard.Against.NullOrWhiteSpace(token);
        Guard.Against.NullOrWhiteSpace(newPassword);

        var results = await _db.QueryAsync<User>(
            "SELECT * FROM users WHERE password_reset_token = @Token AND password_reset_expires_at > @Now LIMIT 1",
            new { Token = token, Now = DateTime.UtcNow });
        var user = results.FirstOrDefault();

        if (user == null)
            return false;

        var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _db.ExecuteAsync(
            "UPDATE users SET password_hash = @Hash, password_reset_token = NULL, password_reset_expires_at = NULL, updated_at = @Now WHERE id = @Id",
            new { Hash = hash, Now = DateTime.UtcNow, Id = user.Id });

        return true;
    }

    /// <inheritdoc />
    public async Task<User> RegisterAsync(string email, string password, string displayName)
    {
        Guard.Against.NullOrWhiteSpace(email);
        Guard.Against.NullOrWhiteSpace(password);
        Guard.Against.NullOrWhiteSpace(displayName);

        var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            DisplayName = displayName.Trim(),
            Role = "viewer",
            IsActive = true,
            EmailVerified = false,
            EmailVerificationToken = verificationToken,
            CreatedAt = DateTime.UtcNow
        };

        await _db.InsertAsync(user);
        return user;
    }

    /// <inheritdoc />
    public async Task<bool> VerifyEmailAsync(string token)
    {
        Guard.Against.NullOrWhiteSpace(token);

        var results = await _db.QueryAsync<User>(
            "SELECT * FROM users WHERE email_verification_token = @Token LIMIT 1",
            new { Token = token });
        var user = results.FirstOrDefault();

        if (user == null)
            return false;

        await _db.ExecuteAsync(
            "UPDATE users SET email_verified = TRUE, email_verification_token = NULL, updated_at = @Now WHERE id = @Id",
            new { Now = DateTime.UtcNow, Id = user.Id });

        return true;
    }
}
