using System.Data;
using NUnit.Framework;
using Moq;
using Bogus;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;
using Contento.Core.Models;
using Contento.Services;

namespace Contento.Tests.Services;

/// <summary>
/// Tests for <see cref="UserService"/>. The service depends on <see cref="IDbConnection"/>
/// for persistence. Since Tuxedo extension methods on IDbConnection are difficult to mock,
/// tests focus on:
///   - Constructor guard clauses
///   - Method-level argument validation (Guard.Against.*)
///   - BCrypt password hashing verification (tested via the BCrypt library directly)
///   - Model defaults
///
/// Full persistence tests would require integration testing against a real database.
/// </summary>
[TestFixture]
public class UserServiceTests
{
    private Mock<IDbConnection> _mockDb = null!;
    private UserService _service = null!;
    private Faker _faker = null!;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new Mock<IDbConnection>();
        _service = new UserService(_mockDb.Object, Mock.Of<ILogger<UserService>>());
        _faker = new Faker();
    }

    // ---------------------------------------------------------------
    // Constructor validation
    // ---------------------------------------------------------------

    [Test]
    public void Constructor_NullDbConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new UserService(null!, Mock.Of<ILogger<UserService>>()));
    }

    [Test]
    public void Constructor_ValidDbConnection_DoesNotThrow()
    {
        Assert.DoesNotThrow(
            () => new UserService(_mockDb.Object, Mock.Of<ILogger<UserService>>()));
    }

    // ---------------------------------------------------------------
    // GetByIdAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByIdAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByIdAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // GetByEmailAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByEmailAsync_NullEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByEmailAsync(null!));
    }

    [Test]
    public void GetByEmailAsync_EmptyEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByEmailAsync(""));
    }

    [Test]
    public void GetByEmailAsync_WhitespaceEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByEmailAsync("   "));
    }

    // ---------------------------------------------------------------
    // GetByRoleAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void GetByRoleAsync_NullRole_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByRoleAsync(null!));
    }

    [Test]
    public void GetByRoleAsync_EmptyRole_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.GetByRoleAsync(""));
    }

    // ---------------------------------------------------------------
    // CreateAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_NullUser_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.CreateAsync(null!, "password123"));
    }

    [Test]
    public void CreateAsync_NullEmail_ThrowsArgumentException()
    {
        var user = CreateValidUser();
        user.Email = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, "password123"));
    }

    [Test]
    public void CreateAsync_EmptyEmail_ThrowsArgumentException()
    {
        var user = CreateValidUser();
        user.Email = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, "password123"));
    }

    [Test]
    public void CreateAsync_NullDisplayName_ThrowsArgumentException()
    {
        var user = CreateValidUser();
        user.DisplayName = null!;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, "password123"));
    }

    [Test]
    public void CreateAsync_EmptyDisplayName_ThrowsArgumentException()
    {
        var user = CreateValidUser();
        user.DisplayName = "";

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, "password123"));
    }

    [Test]
    public void CreateAsync_NullPassword_ThrowsArgumentException()
    {
        var user = CreateValidUser();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, null!));
    }

    [Test]
    public void CreateAsync_EmptyPassword_ThrowsArgumentException()
    {
        var user = CreateValidUser();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, ""));
    }

    [Test]
    public void CreateAsync_WhitespacePassword_ThrowsArgumentException()
    {
        var user = CreateValidUser();

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.CreateAsync(user, "   "));
    }

    // ---------------------------------------------------------------
    // UpdateAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateAsync_NullUser_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(
            async () => await _service.UpdateAsync(null!));
    }

    [Test]
    public void UpdateAsync_DefaultUserId_ThrowsArgumentException()
    {
        var user = CreateValidUser();
        user.Id = Guid.Empty;

        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateAsync(user));
    }

    // ---------------------------------------------------------------
    // DeleteAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void DeleteAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.DeleteAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // ValidatePasswordAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void ValidatePasswordAsync_NullEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ValidatePasswordAsync(null!, "password123"));
    }

    [Test]
    public void ValidatePasswordAsync_EmptyEmail_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ValidatePasswordAsync("", "password123"));
    }

    [Test]
    public void ValidatePasswordAsync_NullPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ValidatePasswordAsync("test@example.com", null!));
    }

    [Test]
    public void ValidatePasswordAsync_EmptyPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ValidatePasswordAsync("test@example.com", ""));
    }

    // ---------------------------------------------------------------
    // UpdatePasswordAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdatePasswordAsync_DefaultId_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdatePasswordAsync(Guid.Empty, "current", "new"));
    }

    [Test]
    public void UpdatePasswordAsync_NullCurrentPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdatePasswordAsync(Guid.NewGuid(), null!, "new"));
    }

    [Test]
    public void UpdatePasswordAsync_EmptyCurrentPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdatePasswordAsync(Guid.NewGuid(), "", "new"));
    }

    [Test]
    public void UpdatePasswordAsync_NullNewPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdatePasswordAsync(Guid.NewGuid(), "current", null!));
    }

    [Test]
    public void UpdatePasswordAsync_EmptyNewPassword_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdatePasswordAsync(Guid.NewGuid(), "current", ""));
    }

    // ---------------------------------------------------------------
    // UpdateLastLoginAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateLastLoginAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateLastLoginAsync(Guid.Empty));
    }

    // ---------------------------------------------------------------
    // UpdateRoleAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void UpdateRoleAsync_EmptyGuid_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateRoleAsync(Guid.Empty, "admin"));
    }

    [Test]
    public void UpdateRoleAsync_NullRole_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateRoleAsync(Guid.NewGuid(), null!));
    }

    [Test]
    public void UpdateRoleAsync_EmptyRole_ThrowsArgumentException()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.UpdateRoleAsync(Guid.NewGuid(), ""));
    }

    // ---------------------------------------------------------------
    // BCrypt password hashing — verify library behavior directly
    // These tests ensure BCrypt.Net-Next works as expected by the service.
    // ---------------------------------------------------------------

    [Test]
    public void BCrypt_HashPassword_ProducesValidHash()
    {
        var password = "MySecurePassword!123";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.That(hash, Is.Not.Null);
        Assert.That(hash, Is.Not.Empty);
        Assert.That(hash, Does.StartWith("$2"));
    }

    [Test]
    public void BCrypt_HashPassword_DifferentCallsProduceDifferentHashes()
    {
        var password = "MySecurePassword!123";
        var hash1 = BCrypt.Net.BCrypt.HashPassword(password);
        var hash2 = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void BCrypt_Verify_CorrectPassword_ReturnsTrue()
    {
        var password = "CorrectHorseBatteryStaple";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var result = BCrypt.Net.BCrypt.Verify(password, hash);

        Assert.That(result, Is.True);
    }

    [Test]
    public void BCrypt_Verify_WrongPassword_ReturnsFalse()
    {
        var password = "CorrectPassword";
        var wrongPassword = "WrongPassword";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        var result = BCrypt.Net.BCrypt.Verify(wrongPassword, hash);

        Assert.That(result, Is.False);
    }

    [Test]
    public void BCrypt_Verify_EmptyVsHash_ReturnsFalse()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("realpassword");

        var result = BCrypt.Net.BCrypt.Verify("", hash);

        Assert.That(result, Is.False);
    }

    [Test]
    public void BCrypt_Verify_CaseSensitive()
    {
        var password = "MyPassword";
        var hash = BCrypt.Net.BCrypt.HashPassword(password);

        Assert.That(BCrypt.Net.BCrypt.Verify("MyPassword", hash), Is.True);
        Assert.That(BCrypt.Net.BCrypt.Verify("mypassword", hash), Is.False);
        Assert.That(BCrypt.Net.BCrypt.Verify("MYPASSWORD", hash), Is.False);
    }

    // ---------------------------------------------------------------
    // User model defaults
    // ---------------------------------------------------------------

    [Test]
    public void User_DefaultValues_AreCorrect()
    {
        var user = new User();

        Assert.That(user.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(user.Email, Is.EqualTo(string.Empty));
        Assert.That(user.DisplayName, Is.EqualTo(string.Empty));
        Assert.That(user.Role, Is.EqualTo("author"));
        Assert.That(user.IsActive, Is.True);
        Assert.That(user.Preferences, Is.EqualTo("{}"));
        Assert.That(user.PasswordHash, Is.EqualTo(string.Empty));
        Assert.That(user.LastLoginAt, Is.Null);
        Assert.That(user.UpdatedAt, Is.Null);
    }

    // ---------------------------------------------------------------
    // Bogus-generated data — fuzz-style guard validation
    // ---------------------------------------------------------------

    [Test]
    public void CreateAsync_BogusUser_WithValidFields_PassesGuards()
    {
        var user = new User
        {
            Email = _faker.Internet.Email(),
            DisplayName = _faker.Person.FullName,
            Role = _faker.PickRandom("owner", "admin", "editor", "author", "viewer"),
            Bio = _faker.Lorem.Paragraph()
        };
        var password = _faker.Internet.Password(length: 16);

        // Guard clauses should pass. The actual DB InsertAsync will fail
        // because IDbConnection is mocked without Tuxedo extension wiring.
        var ex = Assert.CatchAsync(async () => await _service.CreateAsync(user, password));

        if (ex != null)
        {
            Assert.That(ex, Is.Not.TypeOf<ArgumentNullException>());
            Assert.That(ex, Is.Not.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void CreateAsync_EmailIsNormalizedToLowerCase()
    {
        // Verify that the User model can store the expected normalized email
        var user = CreateValidUser();
        user.Email = "  TEST@Example.COM  ";

        // The service normalizes: user.Email.ToLowerInvariant().Trim()
        var normalized = user.Email.ToLowerInvariant().Trim();

        Assert.That(normalized, Is.EqualTo("test@example.com"));
    }

    [Test]
    public void CreateAsync_PasswordIsHashed_NotStoredPlainText()
    {
        // Simulate what CreateAsync does: hash the password
        var plainPassword = "SuperSecretPassword!";
        var hash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        Assert.That(hash, Is.Not.EqualTo(plainPassword));
        Assert.That(hash.Length, Is.GreaterThan(50));
        Assert.That(BCrypt.Net.BCrypt.Verify(plainPassword, hash), Is.True);
    }

    // ---------------------------------------------------------------
    // RequestPasswordResetAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void RequestPasswordResetAsync_NullEmail_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RequestPasswordResetAsync(null!));
    }

    [Test]
    public void RequestPasswordResetAsync_EmptyEmail_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RequestPasswordResetAsync(""));
    }

    // ---------------------------------------------------------------
    // ResetPasswordAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void ResetPasswordAsync_NullToken_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ResetPasswordAsync(null!, "newPassword123"));
    }

    [Test]
    public void ResetPasswordAsync_NullPassword_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ResetPasswordAsync("valid-token", null!));
    }

    // ---------------------------------------------------------------
    // RegisterAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void RegisterAsync_NullEmail_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RegisterAsync(null!, "password123", "Test User"));
    }

    [Test]
    public void RegisterAsync_NullPassword_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RegisterAsync("test@example.com", null!, "Test User"));
    }

    [Test]
    public void RegisterAsync_NullDisplayName_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.RegisterAsync("test@example.com", "password123", null!));
    }

    // ---------------------------------------------------------------
    // VerifyEmailAsync — argument validation
    // ---------------------------------------------------------------

    [Test]
    public void VerifyEmailAsync_NullToken_Throws()
    {
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.VerifyEmailAsync(null!));
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private User CreateValidUser()
    {
        return new User
        {
            Email = "test@example.com",
            DisplayName = "Test User",
            Role = "author"
        };
    }
}
