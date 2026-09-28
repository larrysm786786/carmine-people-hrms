using System.Net;
using System.Net.Mail;
using CarminePeople.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarminePeople.Infrastructure.Notifications;

// Until Smtp:Host is configured, sends are logged instead of dispatched — mirrors
// FirebasePushNotificationSender's fallback so the rest of the pipeline stays testable
// without live SMTP credentials.
public class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly SmtpOptions _options;

    public SmtpEmailSender(ILogger<SmtpEmailSender> logger, IOptions<SmtpOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct, IReadOnlyList<EmailAttachment>? attachments = null)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            _logger.LogInformation("Email (simulated, no SMTP host configured): to={ToEmail}, subject={Subject}, attachments={AttachmentCount}", toEmail, subject, attachments?.Count ?? 0);
            return;
        }

        var attachmentStreams = new List<MemoryStream>();
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(_options.FromEmail, _options.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true,
            };
            message.To.Add(toEmail);

            if (attachments is not null)
            {
                foreach (var attachment in attachments)
                {
                    var stream = new MemoryStream(attachment.Content);
                    attachmentStreams.Add(stream);
                    message.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
                }
            }

            using var client = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                Credentials = string.IsNullOrWhiteSpace(_options.Username)
                    ? null
                    : new NetworkCredential(_options.Username, _options.Password),
            };

            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            // Email delivery is best-effort — a failure here must never take down the
            // caller's flow (e.g. the monthly report job for other employees).
            _logger.LogError(ex, "Email send failed for {ToEmail}", toEmail);
        }
        finally
        {
            // MailMessage.Attachments.Dispose() (via the using block above) closes these
            // streams already, but disposing our own references too is harmless and makes
            // the cleanup explicit rather than relying solely on Attachment's ownership.
            foreach (var stream in attachmentStreams) await stream.DisposeAsync();
        }
    }
}
