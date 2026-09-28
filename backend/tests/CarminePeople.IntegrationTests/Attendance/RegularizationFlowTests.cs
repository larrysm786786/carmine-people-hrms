using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarminePeople.IntegrationTests.Attendance;

public class RegularizationFlowTests : IClassFixture<CarminePeopleApiFactory>
{
    private readonly CarminePeopleApiFactory _factory;
    private const double OfficeLat = 0.0001;
    private const double OfficeLng = 0.0001;

    public RegularizationFlowTests(CarminePeopleApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AuthedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, email);
        return client.WithAuth(token);
    }

    [Fact]
    public async Task RegularizationRequest_ManagerThenHrApproval_UpdatesAttendanceRecord()
    {
        // employee1/employee3 report to manager2 in seed data
        var employeeClient = await AuthedClientAsync("employee1@carminepeople.dev");

        var checkIn = await employeeClient.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        var checkInJson = await checkIn.Content.ReadFromJsonAsync<JsonElement>();
        var recordId = checkInJson.GetProperty("data").GetProperty("id").GetString();

        var regularize = await employeeClient.PostAsJsonAsync($"/api/v1/attendance/{recordId}/regularize", new
        {
            reason = "Forgot to check in on time",
            requestedCheckInAt = DateTime.UtcNow.AddHours(-2)
        });
        Assert.Equal(HttpStatusCode.OK, regularize.StatusCode);
        var regularizeJson = await regularize.Content.ReadFromJsonAsync<JsonElement>();
        var regularizationId = regularizeJson.GetProperty("data").GetProperty("regularizationId").GetString();

        var managerClient = await AuthedClientAsync("manager2@carminepeople.dev");
        var managerApprove = await managerClient.PutAsJsonAsync($"/api/v1/attendance/regularization/{regularizationId}/approve", new { comment = "looks fine" });
        Assert.Equal(HttpStatusCode.OK, managerApprove.StatusCode);

        var hrClient = await AuthedClientAsync("hradmin@carminepeople.dev");
        var hrApprove = await hrClient.PutAsJsonAsync($"/api/v1/attendance/regularization/{regularizationId}/approve", new { comment = "confirmed" });
        Assert.Equal(HttpStatusCode.OK, hrApprove.StatusCode);

        // Re-approving after final HR decision must be rejected.
        var reApprove = await hrClient.PutAsJsonAsync($"/api/v1/attendance/regularization/{regularizationId}/approve", new { comment = "again" });
        Assert.Equal(HttpStatusCode.BadRequest, reApprove.StatusCode);
        var reApproveJson = await reApprove.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("ALREADY_DECIDED", reApproveJson.GetProperty("error").GetProperty("code").GetString());
    }
}
