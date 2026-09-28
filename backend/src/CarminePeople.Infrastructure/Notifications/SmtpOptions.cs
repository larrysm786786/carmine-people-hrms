namespace CarminePeople.Infrastructure.Notifications;

// Bound from the "Smtp" config section (SMTP__Host, SMTP__Username, etc. env vars in
// production/Docker, dotnet user-secrets locally).
public class SmtpOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string FromEmail { get; set; } = "no-reply@carminepeople.local";
    public string FromName { get; set; } = "Carmine People";
}
