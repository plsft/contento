using Contento.Core.Models;

namespace Contento.Core.Interfaces;

/// <summary>
/// Service for managing application users.
/// Handles CRUD operations, authentication helpers, and role-based listing.
/// Password hashing uses BCrypt.Net-Next.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Retrieves a user by their unique identifier.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a user by their email address.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <returns>The user if found; otherwise null.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Retrieves all users with pagination.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of users.</returns>
    Task<IEnumerable<User>> GetAllAsync(int page = 1, int pageSize = 50);

    /// <summary>
    /// Retrieves all users with a specific role, with pagination.
    /// </summary>
    /// <param name="role">The role to filter by (owner, admin, editor, author, viewer).</param>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Number of items per page.</param>
    /// <returns>A paginated collection of users with the specified role.</returns>
    Task<IEnumerable<User>> GetByRoleAsync(string role, int page = 1, int pageSize = 50);

    /// <summary>
    /// Creates a new user. The provided password is hashed using BCrypt before storage.
    /// </summary>
    /// <param name="user">The user to create.</param>
    /// <param name="password">The plain-text password to hash and store.</param>
    /// <returns>The created user with generated identifier.</returns>
    Task<User> CreateAsync(User user, string password);

    /// <summary>
    /// Updates an existing user's profile information.
    /// Does not modify the password; use <see cref="UpdatePasswordAsync"/> for that.
    /// </summary>
    /// <param name="user">The user with updated fields.</param>
    /// <returns>The updated user.</returns>
    Task<User> UpdateAsync(User user);

    /// <summary>
    /// Soft-deletes a user by setting is_active to false.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Validates a user's password against the stored BCrypt hash.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The plain-text password to validate.</param>
    /// <returns>The user if the password is valid; otherwise null.</returns>
    Task<User?> ValidatePasswordAsync(string email, string password);

    /// <summary>
    /// Updates a user's password. The new password is hashed using BCrypt before storage.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="currentPassword">The current plain-text password for verification.</param>
    /// <param name="newPassword">The new plain-text password to hash and store.</param>
    /// <returns>True if the password was updated; false if the current password is incorrect.</returns>
    Task<bool> UpdatePasswordAsync(Guid id, string currentPassword, string newPassword);

    /// <summary>
    /// Updates the last login timestamp for a user to the current UTC time.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    Task UpdateLastLoginAsync(Guid id);

    /// <summary>
    /// Returns the total count of users, optionally filtered by role.
    /// </summary>
    /// <param name="role">Optional role filter.</param>
    /// <returns>The total count of matching users.</returns>
    Task<int> GetTotalCountAsync(string? role = null);

    /// <summary>
    /// Updates a user's role.
    /// </summary>
    /// <param name="id">The user identifier.</param>
    /// <param name="role">The new role (owner, admin, editor, author, viewer).</param>
    Task UpdateRoleAsync(Guid id, string role);

    /// <summary>
    /// Generates a password reset token for the user with the given email.
    /// The token is stored with a 1-hour expiry window.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <returns>The generated password reset token.</returns>
    Task<string> RequestPasswordResetAsync(string email);

    /// <summary>
    /// Resets a user's password using a valid (non-expired) reset token.
    /// </summary>
    /// <param name="token">The password reset token.</param>
    /// <param name="newPassword">The new plain-text password to hash and store.</param>
    /// <returns>True if the password was reset; false if the token is invalid or expired.</returns>
    Task<bool> ResetPasswordAsync(string token, string newPassword);

    /// <summary>
    /// Registers a new public user with the "viewer" role and unverified email.
    /// </summary>
    /// <param name="email">The user's email address.</param>
    /// <param name="password">The plain-text password to hash and store.</param>
    /// <param name="displayName">The user's display name.</param>
    /// <returns>The created user with a verification token set.</returns>
    Task<User> RegisterAsync(string email, string password, string displayName);

    /// <summary>
    /// Verifies a user's email address using the verification token.
    /// </summary>
    /// <param name="token">The email verification token.</param>
    /// <returns>True if the email was verified; false if the token is invalid.</returns>
    Task<bool> VerifyEmailAsync(string token);
}
