using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarminePeople.IntegrationTests.Notifications;

public class NotificationEndpointsTests : IClassFixture<CarminePeopleApiFactory>
{
    private readonly CarminePeopleApiFactory _factory;

    public NotificationEndpointsTests(CarminePeopleApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, email);
        return client.WithAuth(token);
    }

    private static async Task<string> GetLeaveTypeIdAsync(HttpClient client, string name)
    {
        var response = await client.GetAsync("/api/v1/leaves/types");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in json.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == name)
            {
                return item.GetProperty("id").GetString()!;
            }
        }
        throw new InvalidOperationException($"Leave type '{name}' not found in seed data.");
    }

    [Fact]
    public async Task ApplyingForLeave_CreatesNotification_VisibleInList_AndMarkableAsRead()
    {
        var client = await AuthedClientAsync("employee5@carminepeople.dev");
        var casualId = await GetLeaveTypeIdAsync(client, "Casual");

        var apply = await client.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-16", // Monday
            endDate = "2026-11-16",
            reason = "Notification test"
        });
        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);

        var list = await client.GetAsync("/api/v1/notifications?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var listJson = await list.Content.ReadFromJsonAsync<JsonElement>();

        JsonElement? submitted = null;
        foreach (var item in listJson.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("title").GetString() == "Leave request submitted")
            {
                submitted = item;
                break;
            }
        }
        Assert.NotNull(submitted);
        Assert.False(submitted!.Value.GetProperty("isRead").GetBoolean());

        var notificationId = submitted.Value.GetProperty("id").GetString();
        var markRead = await client.PutAsync($"/api/v1/notifications/{notificationId}/read", null);
        Assert.Equal(HttpStatusCode.OK, markRead.StatusCode);

        var listAfter = await client.GetAsync("/api/v1/notifications?page=1&pageSize=20");
        var listAfterJson = await listAfter.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in listAfterJson.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("id").GetString() == notificationId)
            {
                Assert.True(item.GetProperty("isRead").GetBoolean());
            }
        }
    }

    [Fact]
    public async Task Notifications_AreScopedToOwnEmployee_NotVisibleToOthers()
    {
        var applicant = await AuthedClientAsync("employee3@carminepeople.dev");
        var casualId = await GetLeaveTypeIdAsync(applicant, "Casual");

        var apply = await applicant.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-17", // Tuesday
            endDate = "2026-11-17",
            reason = "Scoping test"
        });
        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);

        var otherEmployee = await AuthedClientAsync("employee4@carminepeople.dev");
        var otherList = await otherEmployee.GetAsync("/api/v1/notifications?page=1&pageSize=50");
        var otherJson = await otherList.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var item in otherJson.GetProperty("data").EnumerateArray())
        {
            Assert.NotEqual("Scoping test", item.TryGetProperty("body", out var body) ? body.GetString() : null);
        }
    }
}
