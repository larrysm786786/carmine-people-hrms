namespace CarminePeople.Infrastructure.Notifications;

// Bound from the "Fcm" config section (FCM__ServiceAccountJson env var in production/Docker,
// dotnet user-secrets locally) — the raw JSON content of a Firebase service account key file,
// not a file path, so it works the same way in a container as it does via Key Vault.
public class FcmOptions
{
    public string? ServiceAccountJson { get; set; }
}
