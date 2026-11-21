using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Hms.IntegrationTests;

/// <summary>Test host backed by an isolated PostgreSQL Testcontainer; connection override must be set before host startup.</summary>
public sealed class HmsApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("hms_test")
        .WithUsername("hms")
        .WithPassword("hms_test_password")
        .Build();

    private string? _previousConnection;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__Default");
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _postgres.GetConnectionString());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__Default", _previousConnection);
        await _postgres.DisposeAsync();
        Dispose();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = _postgres.GetConnectionString(),
            ["Database:AutoMigrateAndSeed"] = "true",
            ["Jwt:SigningKey"] = "integration-test-signing-key-at-least-32-characters-long"
        }));
    }
}
