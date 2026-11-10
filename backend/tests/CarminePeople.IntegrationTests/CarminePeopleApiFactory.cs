using CarminePeople.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CarminePeople.IntegrationTests;

// Runs the full API pipeline (including Program.cs's own migrate+seed-on-startup,
// since ASPNETCORE_ENVIRONMENT=Development) against a uniquely-named Postgres
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

    // Local Postgres on localhost:5432 with the default "postgres" superuser by default;
    // CI overrides via TEST_PG_HOST / TEST_PG_PORT / TEST_PG_USER / TEST_PG_PASSWORD to hit
    // the CI Postgres service container.
    private string BuildConnectionString()
    {
        var host = Environment.GetEnvironmentVariable("TEST_PG_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("TEST_PG_PORT") ?? "5432";
        var user = Environment.GetEnvironmentVariable("TEST_PG_USER") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("TEST_PG_PASSWORD") ?? "postgres";

        return $"Host={host};Port={port};Database={_databaseName};Username={user};Password={password}";
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
