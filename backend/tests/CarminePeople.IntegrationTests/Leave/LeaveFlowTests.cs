using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarminePeople.IntegrationTests.Leave;

public class LeaveFlowTests : IClassFixture<CarminePeopleApiFactory>
{
    private readonly CarminePeopleApiFactory _factory;

    public LeaveFlowTests(CarminePeopleApiFactory factory)
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
    public async Task Apply_Approve_IncreasesUsedAndClearsPending()
    {
        // employee2/employee4 report to manager1 in seed data
        var employeeClient = await AuthedClientAsync("employee2@carminepeople.dev");
        var casualId = await GetLeaveTypeIdAsync(employeeClient, "Casual");

        var apply = await employeeClient.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-02", // Monday
            endDate = "2026-11-03",   // Tuesday -> 2 working days
            reason = "Personal"
        });
        Assert.Equal(HttpStatusCode.OK, apply.StatusCode);
        var applyJson = await apply.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, applyJson.GetProperty("data").GetProperty("daysCount").GetDecimal());
        var leaveId = applyJson.GetProperty("data").GetProperty("id").GetString();

        var balanceBefore = await GetBalanceAsync(employeeClient, casualId);
        Assert.Equal(2, balanceBefore.GetProperty("pending").GetDecimal());
        Assert.Equal(0, balanceBefore.GetProperty("used").GetDecimal());

        // Two-stage chain: a Manager's approval only advances status to ManagerApproved and
        // does not touch the balance yet — only HR/SuperAdmin finalizes it (isFinal in
        // LeaveService.DecideAsync). So pending stays put after the manager's decision.
        var managerClient = await AuthedClientAsync("manager1@carminepeople.dev");
        var managerApprove = await managerClient.PutAsJsonAsync($"/api/v1/leaves/{leaveId}/approve", new { comment = "ok" });
        Assert.Equal(HttpStatusCode.OK, managerApprove.StatusCode);

        var balanceAfterManager = await GetBalanceAsync(employeeClient, casualId);
        Assert.Equal(2, balanceAfterManager.GetProperty("pending").GetDecimal());
        Assert.Equal(0, balanceAfterManager.GetProperty("used").GetDecimal());

        var hrClient = await AuthedClientAsync("hradmin@carminepeople.dev");
        var hrApprove = await hrClient.PutAsJsonAsync($"/api/v1/leaves/{leaveId}/approve", new { comment = "confirmed" });
        Assert.Equal(HttpStatusCode.OK, hrApprove.StatusCode);

        var balanceAfter = await GetBalanceAsync(employeeClient, casualId);
        Assert.Equal(0, balanceAfter.GetProperty("pending").GetDecimal());
        Assert.Equal(2, balanceAfter.GetProperty("used").GetDecimal());
    }

    [Fact]
    public async Task Apply_WeekendOnlyRange_IsRejectedForZeroWorkingDays()
    {
        var client = await AuthedClientAsync("employee3@carminepeople.dev");
        var casualId = await GetLeaveTypeIdAsync(client, "Casual");

        // 2026-11-07 = Saturday, 2026-11-08 = Sunday
        var apply = await client.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-07",
            endDate = "2026-11-08",
            reason = "weekend only"
        });

        Assert.Equal(HttpStatusCode.BadRequest, apply.StatusCode);
        var json = await apply.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NO_WORKING_DAYS", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Apply_OverlappingExistingPendingApplication_IsRejected()
    {
        var client = await AuthedClientAsync("employee5@carminepeople.dev");
        var casualId = await GetLeaveTypeIdAsync(client, "Casual");

        var first = await client.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-23", // Monday
            endDate = "2026-11-24",   // Tuesday
            reason = "First request"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var overlapping = await client.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-24", // overlaps the Tuesday above
            endDate = "2026-11-25",
            reason = "Overlaps first request"
        });

        Assert.Equal(HttpStatusCode.BadRequest, overlapping.StatusCode);
        var json = await overlapping.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OVERLAPPING_LEAVE", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Cancel_PendingApplication_RestoresPendingBalance()
    {
        var client = await AuthedClientAsync("employee4@carminepeople.dev");
        var casualId = await GetLeaveTypeIdAsync(client, "Casual");

        var apply = await client.PostAsJsonAsync("/api/v1/leaves", new
        {
            leaveTypeId = casualId,
            startDate = "2026-11-09", // Monday
            endDate = "2026-11-09",
            reason = "single day"
        });
        var applyJson = await apply.Content.ReadFromJsonAsync<JsonElement>();
        var leaveId = applyJson.GetProperty("data").GetProperty("id").GetString();

        var cancel = await client.PutAsync($"/api/v1/leaves/{leaveId}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);

        var balance = await GetBalanceAsync(client, casualId);
        Assert.Equal(0, balance.GetProperty("pending").GetDecimal());
    }

    private static async Task<JsonElement> GetBalanceAsync(HttpClient client, string leaveTypeId)
    {
        var response = await client.GetAsync("/api/v1/leaves/balances");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in json.GetProperty("data").EnumerateArray())
        {
            if (item.GetProperty("leaveTypeId").GetString() == leaveTypeId)
            {
                return item;
            }
        }
        throw new InvalidOperationException("Balance not found for leave type.");
    }
}
