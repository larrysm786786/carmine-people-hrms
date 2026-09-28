using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarminePeople.IntegrationTests.Auth;

public class AuthEndpointsTests : IClassFixture<CarminePeopleApiFactory>
{
    private readonly CarminePeopleApiFactory _factory;

    public AuthEndpointsTests(CarminePeopleApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndProfile()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "superadmin@carminepeople.dev", password = TestAuthHelper.SeedPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        Assert.False(string.IsNullOrEmpty(json.GetProperty("data").GetProperty("accessToken").GetString()));
        Assert.Equal("SuperAdmin", json.GetProperty("data").GetProperty("employee").GetProperty("roleName").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "superadmin@carminepeople.dev", password = "wrong-password" });
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithValidToken_ReturnsClaims()
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, "employee1@carminepeople.dev");
        client.WithAuth(token);

        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_RotatesToken_OldTokenBecomesInvalid()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "manager1@carminepeople.dev", password = TestAuthHelper.SeedPassword });
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginJson.GetProperty("data").GetProperty("refreshToken").GetString();

        var firstRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, firstRefresh.StatusCode);

        var secondRefresh = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, secondRefresh.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesRefreshToken_SubsequentRefreshFails()
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = "employee2@carminepeople.dev", password = TestAuthHelper.SeedPassword });
        var loginJson = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginJson.GetProperty("data").GetProperty("refreshToken").GetString();

        var logout = await client.PostAsJsonAsync("/api/v1/auth/logout", new { refreshToken });
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
        var logoutJson = await logout.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(logoutJson.GetProperty("success").GetBoolean());

        var refreshAfterLogout = await client.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refreshAfterLogout.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_UnknownEmail_StillReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/forgot-password", new { email = "definitely-not-registered@nowhere.com" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
