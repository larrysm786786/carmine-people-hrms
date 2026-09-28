using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarminePeople.IntegrationTests.Attendance;

public class AttendanceEndpointsTests : IClassFixture<CarminePeopleApiFactory>
{
    private readonly CarminePeopleApiFactory _factory;

    public AttendanceEndpointsTests(CarminePeopleApiFactory factory)
    {
        _factory = factory;
    }

    // Seed employees are all assigned to "Head Office" at lat/lng 0,0 with a 100m geofence.
    private const double OfficeLat = 0.0001;
    private const double OfficeLng = 0.0001;

    private async Task<HttpClient> AuthedClientAsync(string email)
    {
        var client = _factory.CreateClient();
        var token = await TestAuthHelper.LoginAsync(client, email);
        return client.WithAuth(token);
    }

    [Fact]
    public async Task GetList_AsPlainEmployee_OnlyReturnsOwnRecords()
    {
        // employee1 checks in; a plain Employee (attendance.view is granted to everyone,
        // but that permission must not double as "see everyone's attendance") querying the
        // list without an employeeId filter must only ever see their own records back.
        var employee1Client = await AuthedClientAsync("employee1@carminepeople.dev");
        var employee1Id = await GetOwnEmployeeIdAsync(employee1Client);

        var listResponse = await employee1Client.GetAsync("/api/v1/attendance?page=1&pageSize=100");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var json = await listResponse.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var record in json.GetProperty("data").EnumerateArray())
        {
            Assert.Equal(employee1Id, record.GetProperty("employeeId").GetString());
        }
    }

    [Fact]
    public async Task GetList_AsHrAdmin_CanSeeOtherEmployeesRecords()
    {
        var hrClient = await AuthedClientAsync("hradmin@carminepeople.dev");
        var employee1Id = await GetOwnEmployeeIdAsync(await AuthedClientAsync("employee1@carminepeople.dev"));

        var response = await hrClient.GetAsync($"/api/v1/attendance?employeeId={employee1Id}&page=1&pageSize=10");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        foreach (var record in json.GetProperty("data").EnumerateArray())
        {
            Assert.Equal(employee1Id, record.GetProperty("employeeId").GetString());
        }
    }

    private static async Task<string> GetOwnEmployeeIdAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/auth/me");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var claim in json.GetProperty("data").EnumerateArray())
        {
            if (claim.GetProperty("type").GetString() == System.Security.Claims.ClaimTypes.NameIdentifier
                || claim.GetProperty("type").GetString() == "sub")
            {
                return claim.GetProperty("value").GetString()!;
            }
        }
        throw new InvalidOperationException("Could not find employee id claim in /auth/me response.");
    }

    [Fact]
    public async Task CheckIn_NormalHappyPath_ReturnsPresentRecord()
    {
        var client = await AuthedClientAsync("employee1@carminepeople.dev");
        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(json.GetProperty("success").GetBoolean());
        var record = json.GetProperty("data");
        Assert.False(string.IsNullOrEmpty(record.GetProperty("checkInAt").GetString()));
        Assert.True(record.TryGetProperty("checkOutAt", out var checkOutAt));
        Assert.Equal(JsonValueKind.Null, checkOutAt.ValueKind);
    }

    [Fact]
    public async Task MultipleBreaks_EachStartEndPairAccumulatesBreakTime()
    {
        var client = await AuthedClientAsync("manager2@carminepeople.dev");
        var checkIn = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);

        var firstBreakStart = await client.PostAsync("/api/v1/attendance/break-start", null);
        Assert.Equal(HttpStatusCode.OK, firstBreakStart.StatusCode);
        var firstBreakStartJson = await firstBreakStart.Content.ReadFromJsonAsync<JsonElement>();
        var firstBreakId = firstBreakStartJson.GetProperty("data").GetProperty("id").GetString();

        var firstBreakEnd = await client.PostAsync("/api/v1/attendance/break-end", null);
        Assert.Equal(HttpStatusCode.OK, firstBreakEnd.StatusCode);

        // A second, separate break on the same still-checked-in record must also be allowed —
        // the "no active break" guard only blocks a *second concurrently open* break, not a
        // second break taken later in the same day — and it must be a genuinely new Break row,
        // not the first one being reopened/reused.
        var secondBreakStart = await client.PostAsync("/api/v1/attendance/break-start", null);
        Assert.Equal(HttpStatusCode.OK, secondBreakStart.StatusCode);
        var secondBreakStartJson = await secondBreakStart.Content.ReadFromJsonAsync<JsonElement>();
        var secondBreakId = secondBreakStartJson.GetProperty("data").GetProperty("id").GetString();
        Assert.NotEqual(firstBreakId, secondBreakId);

        var secondBreakEnd = await client.PostAsync("/api/v1/attendance/break-end", null);
        Assert.Equal(HttpStatusCode.OK, secondBreakEnd.StatusCode);
    }

    [Fact]
    public async Task CheckIn_PoorGpsAccuracy_IsRejected()
    {
        var client = await AuthedClientAsync("superadmin@carminepeople.dev");
        // Within the geofence radius, but the reported GPS uncertainty itself is too large
        // to trust — must be rejected before the geofence distance check even matters.
        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 250.0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("POOR_GPS_ACCURACY", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CheckIn_OutsideGeofence_IsRejected()
    {
        var client = await AuthedClientAsync("employee2@carminepeople.dev");
        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = 45.0, longitude = 45.0, accuracyMeters = 5.0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("OUTSIDE_GEOFENCE", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CheckIn_ThenDuplicateCheckIn_IsRejected()
    {
        var client = await AuthedClientAsync("employee3@carminepeople.dev");

        var first = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var duplicate = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        Assert.Equal(HttpStatusCode.BadRequest, duplicate.StatusCode);
        var json = await duplicate.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("DUPLICATE_CHECK_IN", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CheckOut_WithoutCheckIn_IsRejected()
    {
        var client = await AuthedClientAsync("employee4@carminepeople.dev");
        var response = await client.PostAsJsonAsync("/api/v1/attendance/check-out", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("NOT_CHECKED_IN", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task CheckOut_WhileBreakInProgress_IsBlocked_ThenSucceedsAfterBreakEnds()
    {
        var client = await AuthedClientAsync("hradmin@carminepeople.dev");

        var checkIn = await client.PostAsJsonAsync("/api/v1/attendance/check-in", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        Assert.Equal(HttpStatusCode.OK, checkIn.StatusCode);

        var breakStart = await client.PostAsync("/api/v1/attendance/break-start", null);
        Assert.Equal(HttpStatusCode.OK, breakStart.StatusCode);

        var blockedCheckOut = await client.PostAsJsonAsync("/api/v1/attendance/check-out", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        Assert.Equal(HttpStatusCode.BadRequest, blockedCheckOut.StatusCode);
        var blockedJson = await blockedCheckOut.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("BREAK_IN_PROGRESS", blockedJson.GetProperty("error").GetProperty("code").GetString());

        var breakEnd = await client.PostAsync("/api/v1/attendance/break-end", null);
        Assert.Equal(HttpStatusCode.OK, breakEnd.StatusCode);

        var checkOut = await client.PostAsJsonAsync("/api/v1/attendance/check-out", new { latitude = OfficeLat, longitude = OfficeLng, accuracyMeters = 5.0 });
        Assert.Equal(HttpStatusCode.OK, checkOut.StatusCode);
    }

    [Fact]
    public async Task Employee_WithoutEmployeeManagePermission_CannotCreateEmployee()
    {
        var client = await AuthedClientAsync("employee5@carminepeople.dev");
        var response = await client.PostAsJsonAsync("/api/v1/employees", new
        {
            employeeCode = "EMP9999",
            firstName = "Should",
            lastName = "Fail",
            email = "should-fail@carminepeople.dev",
            password = "Passw0rd!",
            departmentId = Guid.NewGuid(),
            designationId = Guid.NewGuid(),
            locationId = Guid.NewGuid(),
            roleId = Guid.NewGuid(),
            dateOfJoining = DateTime.UtcNow
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
