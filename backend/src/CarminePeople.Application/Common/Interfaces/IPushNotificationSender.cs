namespace CarminePeople.Application.Common.Interfaces;

public interface IPushNotificationSender
{
    Task SendAsync(IEnumerable<string> deviceTokens, string title, string body, string type, string? deepLinkPath, CancellationToken ct);
}
