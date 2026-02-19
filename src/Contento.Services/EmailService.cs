using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Contento.Core.Interfaces;

namespace Contento.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
    {
        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host) || host.StartsWith("${"))
        {
            _logger.LogDebug("SMTP not configured, skipping email to {To}: {Subject}", to, subject);
            return;
        }

        var message = BuildMessage(to, subject, htmlBody);

        using var client = new SmtpClient();
        await ConnectAndAuthenticateAsync(client, ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);

        _logger.LogInformation("Email sent to {To}: {Subject}", to, subject);
    }

    public async Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody,
        Func<string, string>? personalizeBody = null, CancellationToken ct = default)
    {
        var host = _configuration["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host) || host.StartsWith("${"))
        {
            _logger.LogDebug("SMTP not configured, skipping bulk email: {Subject}", subject);
            return;
        }

        using var client = new SmtpClient();
        await ConnectAndAuthenticateAsync(client, ct);

        var sent = 0;
        var failed = 0;

        foreach (var recipient in recipients)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                var body = personalizeBody != null ? personalizeBody(recipient) : htmlBody;
                var message = BuildMessage(recipient, subject, body);

                // Add List-Unsubscribe headers
                message.Headers.Add("List-Unsubscribe-Post", "List-Unsubscribe=One-Click");

                await client.SendAsync(message, ct);
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send email to {Recipient}", recipient);
                failed++;
            }
        }

        await client.DisconnectAsync(true, ct);
        _logger.LogInformation("Bulk email complete: {Sent} sent, {Failed} failed — {Subject}", sent, failed, subject);
    }

    private MimeMessage BuildMessage(string to, string subject, string htmlBody)
    {
        var fromAddress = _configuration["Smtp:From"] ?? "noreply@contento.local";
        var fromName = _configuration["Smtp:FromName"] ?? "Contento";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        return message;
    }

    private async Task ConnectAndAuthenticateAsync(SmtpClient client, CancellationToken ct)
    {
        var host = _configuration["Smtp:Host"]!;
        var port = int.TryParse(_configuration["Smtp:Port"], out var p) ? p : 587;
        var username = _configuration["Smtp:Username"];
        var password = _configuration["Smtp:Password"];

        var useSsl = port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(host, port, useSsl, ct);

        if (!string.IsNullOrWhiteSpace(username) && !username.StartsWith("${"))
        {
            await client.AuthenticateAsync(username, password, ct);
        }
    }
}
