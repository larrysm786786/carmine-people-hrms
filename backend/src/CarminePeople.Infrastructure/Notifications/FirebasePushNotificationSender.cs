using CarminePeople.Application.Common.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CarminePeople.Infrastructure.Notifications;

// Wraps Firebase Cloud Messaging delivery. Requires a Firebase service account
// (Fcm:ServiceAccountJson — the FCM__ServiceAccountJson env var, or dotnet user-secrets
// locally) to send for real. Until credentials are configured, sends are logged instead of
// dispatched so the rest of the notification pipeline (in-app records, preferences) is
// fully testable without a live Firebase project.
public class FirebasePushNotificationSender : IPushNotificationSender
{
    private readonly ILogger<FirebasePushNotificationSender> _logger;
    private readonly FcmOptions _options;

    // FirebaseApp.Create can only be called once per process; lazily initialize on first
    // real send rather than at startup, so a missing/invalid credential doesn't crash the
    // whole API — it just falls back to log-only sends.
    private static FirebaseApp? _app;
    private static readonly object _initLock = new();

    public FirebasePushNotificationSender(ILogger<FirebasePushNotificationSender> logger, IOptions<FcmOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public async Task SendAsync(IEnumerable<string> deviceTokens, string title, string body, string type, string? deepLinkPath, CancellationToken ct)
    {
        var tokens = deviceTokens.ToList();
        if (tokens.Count == 0)
        {
            return;
        }

        var app = GetOrCreateApp();
        if (app is null)
        {
            _logger.LogInformation(
                "Push notification (simulated, no FCM credentials configured): {Title} to {TokenCount} device(s), type={Type}, deepLink={DeepLink}",
                title, tokens.Count, type, deepLinkPath);
            return;
        }

        // `type`/deepLinkPath live in `data` (not `notification`) so the mobile app's tap
        // handler can read message.data['type'] to route in-app — see
        // FirebaseMessagingService._handleNotificationTap on the mobile side. `notification`
        // carries the title/body/sound the OS uses to render the system tray entry and play
        // the default notification tone while the app is backgrounded/terminated; the
        // `AndroidConfig`/`ApnsConfig` blocks additionally request the badge/sound explicitly
        // per platform since a bare `notification` payload doesn't guarantee either.
#pragma warning disable CS0618 // MulticastMessage.Tokens is the documented way to target a token list in FirebaseAdmin 3.6; Fids is a newer registration-token concept, not a drop-in replacement.
        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification { Title = title, Body = body },
            Data = new Dictionary<string, string>
            {
                ["type"] = type,
                ["deepLinkPath"] = deepLinkPath ?? "",
            },
            Android = new AndroidConfig
            {
                // ChannelId must exactly match the channel the mobile app creates on launch
                // (FirebaseMessagingService._defaultChannel) — on Android 8+ the OS ignores
                // this message's own Sound field and instead plays whatever that channel was
                // configured with at creation time, so routing to the right channel is what
                // actually determines whether the device makes any sound at all.
                Notification = new AndroidNotification { Sound = "default", ChannelId = "carmine_people_default" },
            },
            Apns = new ApnsConfig
            {
                Aps = new Aps { Sound = "default", Badge = 1 },
            },
        };
#pragma warning restore CS0618

        try
        {
            var response = await FirebaseMessaging.GetMessaging(app).SendEachForMulticastAsync(message, ct);
            if (response.FailureCount > 0)
            {
                _logger.LogWarning("FCM push: {SuccessCount} succeeded, {FailureCount} failed out of {TokenCount}",
                    response.SuccessCount, response.FailureCount, tokens.Count);
            }
        }
        catch (Exception ex)
        {
            // Push delivery is best-effort — the in-app notification record already exists
            // regardless of whether the OS-level push reaches the device, so a delivery
            // failure here must never surface as an error to the caller.
            _logger.LogError(ex, "FCM push send failed for {TokenCount} device(s)", tokens.Count);
        }
    }

    private FirebaseApp? GetOrCreateApp()
    {
        if (_app is not null) return _app;
        if (string.IsNullOrWhiteSpace(_options.ServiceAccountJson)) return null;

        lock (_initLock)
        {
            if (_app is not null) return _app;
            try
            {
#pragma warning disable CS0618 // FromJson is still the documented path for an in-memory service-account JSON string (not a file path); the suggested CredentialFactory replacement targets file-based credentials.
                _app = FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromJson(_options.ServiceAccountJson),
                });
#pragma warning restore CS0618
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Firebase Admin SDK — check Fcm:ServiceAccountJson is valid JSON.");
                return null;
            }
        }
        return _app;
    }
}
