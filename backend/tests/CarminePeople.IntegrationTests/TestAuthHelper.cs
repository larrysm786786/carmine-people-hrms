using System.Net.Http.Json;
using System.Text.Json;

namespace CarminePeople.IntegrationTests;

public static class TestAuthHelper
{
    public const string SeedPassword = "Passw0rd!";

    public static async Task<string> LoginAsync(HttpClient client, string email, string password = SeedPassword)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("data").GetProperty("accessToken").GetString()!;
    }

    public static HttpClient WithAuth(this HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
