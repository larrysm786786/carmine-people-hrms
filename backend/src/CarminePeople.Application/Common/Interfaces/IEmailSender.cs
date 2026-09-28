namespace CarminePeople.Application.Common.Interfaces;

public record EmailAttachment(string FileName, byte[] Content, string ContentType);

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct, IReadOnlyList<EmailAttachment>? attachments = null);
}
