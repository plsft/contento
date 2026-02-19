using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<RegisterModel> _logger;

    public RegisterModel(IUserService userService, IEmailService emailService, ILogger<RegisterModel> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string Password { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    [BindProperty]
    public string DisplayName { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(DisplayName) || string.IsNullOrWhiteSpace(Email) ||
            string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "All fields are required.";
            return Page();
        }

        if (Password != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        if (Password.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters.";
            return Page();
        }

        try
        {
            var existingUser = await _userService.GetByEmailAsync(Email.Trim().ToLowerInvariant());
            if (existingUser != null)
            {
                ErrorMessage = "An account with this email already exists.";
                return Page();
            }

            var user = await _userService.RegisterAsync(Email, Password, DisplayName);

            // Send verification email
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var verifyUrl = $"{baseUrl}/verify-email?token={user.EmailVerificationToken}";
            var htmlBody = $@"
                <h2>Welcome to Contento!</h2>
                <p>Hi {user.DisplayName},</p>
                <p>Thanks for creating an account. Please verify your email address by clicking the link below:</p>
                <p><a href=""{verifyUrl}"" style=""display:inline-block;padding:12px 24px;background:#4f46e5;color:#fff;text-decoration:none;border-radius:6px;"">Verify Email</a></p>
                <p>If the button doesn't work, copy and paste this URL into your browser:</p>
                <p>{verifyUrl}</p>
                <p>This link will expire in 24 hours.</p>";

            await _emailService.SendAsync(user.Email, "Verify your email - Contento", htmlBody);

            _logger.LogInformation("New user registered: {Email}", user.Email);

            SuccessMessage = "Check your email to verify your account.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register user in {Page}", nameof(RegisterModel));
            ErrorMessage = "An error occurred during registration. Please try again.";
            return Page();
        }
    }
}
