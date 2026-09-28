using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace CarminePeople.IntegrationTests.Employees;

public class EmployeeEndpointsTests : IClassFixture<CarminePeopleApiFactory>
{
    private readonly CarminePeopleApiFactory _factory;

    public EmployeeEndpointsTests(CarminePeopleApiFactory factory)
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
    public async Task Employee_WithEmployeeManagePermission_CanCreateEmployee()
    {
        var hrClient = await AuthedClientAsync("hradmin@carminepeople.dev");

        // Reuse an existing seeded employee's org assignment (department/designation/location/role)
        // so the test doesn't need to know seed GUIDs directly.
        var listResponse = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=1");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var seedEmployeeId = listJson.GetProperty("data")[0].GetProperty("id").GetString();

        var detailResponse = await hrClient.GetAsync($"/api/v1/employees/{seedEmployeeId}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);
        var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var create = await hrClient.PostAsJsonAsync("/api/v1/employees", new
        {
            employeeCode = $"EMP{uniqueSuffix}",
            firstName = "New",
            lastName = "Hire",
            email = $"new.hire.{uniqueSuffix}@carminepeople.dev",
            password = "Passw0rd!",
            phoneNumber = (string?)null,
            departmentId = detail.GetProperty("departmentId").GetString(),
            designationId = detail.GetProperty("designationId").GetString(),
            locationId = detail.GetProperty("locationId").GetString(),
            managerId = (string?)null,
            roleId = detail.GetProperty("roleId").GetString(),
            dateOfJoining = DateTime.UtcNow,
            dateOfBirth = (DateTime?)null
        });

        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var createdJson = await create.Content.ReadFromJsonAsync<JsonElement>();
        var created = createdJson.GetProperty("data");
        Assert.Equal($"EMP{uniqueSuffix}", created.GetProperty("employeeCode").GetString());

        // Newly created employee can log in with the password supplied at creation.
        var newEmployeeClient = _factory.CreateClient();
        var loginToken = await TestAuthHelper.LoginAsync(newEmployeeClient, $"new.hire.{uniqueSuffix}@carminepeople.dev");
        Assert.False(string.IsNullOrWhiteSpace(loginToken));
    }

    [Fact]
    public async Task Employee_UpdateWithSelfAsManager_IsRejected()
    {
        var hrClient = await AuthedClientAsync("hradmin@carminepeople.dev");

        var listResponse = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=1");
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var employeeId = listJson.GetProperty("data")[0].GetProperty("id").GetString();
        var detailResponse = await hrClient.GetAsync($"/api/v1/employees/{employeeId}");
        var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var update = await hrClient.PutAsJsonAsync($"/api/v1/employees/{employeeId}", new
        {
            firstName = detail.GetProperty("firstName").GetString(),
            lastName = detail.GetProperty("lastName").GetString(),
            phoneNumber = (string?)null,
            departmentId = detail.GetProperty("departmentId").GetString(),
            designationId = detail.GetProperty("designationId").GetString(),
            locationId = detail.GetProperty("locationId").GetString(),
            managerId = employeeId, // self-reference
            roleId = detail.GetProperty("roleId").GetString(),
            dateOfBirth = (DateTime?)null,
            employmentStatus = detail.GetProperty("employmentStatus").GetInt32()
        });

        var json = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("INVALID_MANAGER", json.GetProperty("error").GetProperty("code").GetString());
    }

    [Fact]
    public async Task Employee_CreateWithDuplicateEmail_IsRejected()
    {
        var hrClient = await AuthedClientAsync("hradmin@carminepeople.dev");

        var listResponse = await hrClient.GetAsync("/api/v1/employees?page=1&pageSize=1");
        var listJson = await listResponse.Content.ReadFromJsonAsync<JsonElement>();
        var seedEmployeeId = listJson.GetProperty("data")[0].GetProperty("id").GetString();
        var detailResponse = await hrClient.GetAsync($"/api/v1/employees/{seedEmployeeId}");
        var detail = (await detailResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("data");

        var response = await hrClient.PostAsJsonAsync("/api/v1/employees", new
        {
            employeeCode = $"EMP{Guid.NewGuid():N}"[..11],
            firstName = "Dup",
            lastName = "Email",
            email = "hradmin@carminepeople.dev", // already exists in seed data
            password = "Passw0rd!",
            phoneNumber = (string?)null,
            departmentId = detail.GetProperty("departmentId").GetString(),
            designationId = detail.GetProperty("designationId").GetString(),
            locationId = detail.GetProperty("locationId").GetString(),
            managerId = (string?)null,
            roleId = detail.GetProperty("roleId").GetString(),
            dateOfJoining = DateTime.UtcNow,
            dateOfBirth = (DateTime?)null
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("EMAIL_TAKEN", json.GetProperty("error").GetProperty("code").GetString());
    }
}
