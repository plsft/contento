namespace Contento.Core.Interfaces;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    Task SendBulkAsync(IEnumerable<string> recipients, string subject, string htmlBody,
        Func<string, string>? personalizeBody = null, CancellationToken ct = default);
}
