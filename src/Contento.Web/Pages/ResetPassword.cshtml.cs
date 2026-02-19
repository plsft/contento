using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Contento.Core.Interfaces;

namespace Contento.Web.Pages;

[AllowAnonymous]
public class ResetPasswordModel : PageModel
{
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ResetPasswordModel> _logger;

    public ResetPasswordModel(IUserService userService, IEmailService emailService, ILogger<ResetPasswordModel> logger)
    {
        _userService = userService;
        _emailService = emailService;
        _logger = logger;
    }

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string? Token { get; set; }

    [BindProperty]
    public string NewPassword { get; set; } = string.Empty;

    [BindProperty]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public bool HasToken => !string.IsNullOrEmpty(Token);

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostRequestResetAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Please enter your email address.";
            return Page();
        }

        try
        {
            var token = await _userService.RequestPasswordResetAsync(Email.Trim().ToLowerInvariant());

            // Send reset email (only if a real token was returned, i.e. user exists)
            if (!string.IsNullOrEmpty(token))
            {
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var resetUrl = $"{baseUrl}/reset-password?token={token}";
                var htmlBody = $@"
                    <h2>Password Reset</h2>
                    <p>We received a request to reset your password. Click the link below to set a new password:</p>
                    <p><a href=""{resetUrl}"" style=""display:inline-block;padding:12px 24px;background:#4f46e5;color:#fff;text-decoration:none;border-radius:6px;"">Reset Password</a></p>
                    <p>If the button doesn't work, copy and paste this URL into your browser:</p>
                    <p>{resetUrl}</p>
                    <p>This link will expire in 1 hour.</p>
                    <p>If you didn't request this, you can safely ignore this email.</p>";

                await _emailService.SendAsync(Email.Trim(), "Reset your password - Contento", htmlBody);
            }

            _logger.LogInformation("Password reset requested for: {Email}", Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process password reset request in {Page}", nameof(ResetPasswordModel));
        }

        // Always show success message to prevent email enumeration
        SuccessMessage = "If an account with that email exists, we've sent a password reset link.";
        return Page();
    }

    public async Task<IActionResult> OnPostResetPasswordAsync()
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            ErrorMessage = "Invalid or missing reset token.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(NewPassword) || string.IsNullOrWhiteSpace(ConfirmPassword))
        {
            ErrorMessage = "Please enter and confirm your new password.";
            return Page();
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match.";
            return Page();
        }

        if (NewPassword.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters.";
            return Page();
        }

        try
        {
            var result = await _userService.ResetPasswordAsync(Token, NewPassword);

            if (!result)
            {
                ErrorMessage = "This reset link is invalid or has expired. Please request a new one.";
                return Page();
            }

            _logger.LogInformation("Password successfully reset via token");

            SuccessMessage = "Your password has been reset. You can now sign in with your new password.";
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reset password in {Page}", nameof(ResetPasswordModel));
            ErrorMessage = "An error occurred. Please try again.";
            return Page();
        }
    }
}
