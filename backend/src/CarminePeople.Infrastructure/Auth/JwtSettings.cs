namespace CarminePeople.Infrastructure.Auth;

public class JwtSettings
{
    public string SigningKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CarminePeople";
    public string Audience { get; set; } = "CarminePeopleClients";
    public int AccessTokenMinutes { get; set; } = 15;
}
