using CarminePeople.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarminePeople.IntegrationTests;

// Runs the full API pipeline (including Program.cs's own migrate+seed-on-startup,
// since ASPNETCORE_ENVIRONMENT=Development) against a uniquely-named SQL Server
// database per test class instance, so integration tests can rely on real
// constraints (unique indexes, FK behavior) that the InMemory provider does not enforce.
public class CarminePeopleApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _databaseName = $"CarminePeopleDb_Test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = BuildConnectionString(),
                ["Jwt:SigningKey"] = "test-signing-key-at-least-32-characters-long",
            });
        });
    }

    // Windows dev (Trusted_Connection) by default; CI overrides via TEST_SQL_SERVER /
    // TEST_SQL_USER / TEST_SQL_PASSWORD to hit the CI SQL Server container over SA auth.
    private string BuildConnectionString()
    {
        var server = Environment.GetEnvironmentVariable("TEST_SQL_SERVER") ?? "localhost";
        var user = Environment.GetEnvironmentVariable("TEST_SQL_USER");
        var password = Environment.GetEnvironmentVariable("TEST_SQL_PASSWORD");

        if (!string.IsNullOrEmpty(user))
        {
            return $"Server={server};Database={_databaseName};User Id={user};Password={password};TrustServerCertificate=True";
        }
        return $"Server={server};Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
    }

    public Task InitializeAsync()
    {
        // Triggers host startup, which runs Program.cs's migrate+seed block.
        _ = Server;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.EnsureDeletedAsync();
    }
}
