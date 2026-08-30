using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Knowledge.Server.IntegrationTests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Liveness_DoesNotDependOnStorage()
    {
        const string secret = "do-not-expose-this";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "PostgreSql",
            ["Persistence:PostgreSqlConnectionString"] =
                $"Host=127.0.0.1;Port=1;Database=unavailable;Username=none;Password={secret};Timeout=1",
        });

        var response = await factory.CreateClient().GetAsync("/health/live", CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(secret, body);
    }

    [Fact]
    public async Task Readiness_ReturnsUnavailableWithoutExposingConnectionDetails()
    {
        const string secret = "do-not-expose-this";
        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "PostgreSql",
            ["Persistence:PostgreSqlConnectionString"] =
                $"Host=127.0.0.1;Port=1;Database=unavailable;Username=none;Password={secret};Timeout=1",
        });

        var response = await factory.CreateClient().GetAsync("/health/ready", CancellationToken.None);
        var body = await response.Content.ReadAsStringAsync(CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.DoesNotContain(secret, body);
        Assert.DoesNotContain("127.0.0.1", body);
    }

    [Fact]
    public async Task SqliteReadiness_ReportsSelectedProvider()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knowledge-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = CreateFactory(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "Sqlite",
                ["Persistence:SqliteConnectionString"] = $"Data Source={databasePath}",
            });

            var response = await factory.CreateClient().GetAsync(
                "/health/ready",
                CancellationToken.None);
            var body = await response.Content.ReadFromJsonAsync<HealthResponse>(
                CancellationToken.None);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(body);
            Assert.Contains(body.Checks, check =>
                check.Name == "persistence" &&
                check.Data.TryGetValue("provider", out var provider) &&
                provider.GetString() == "Sqlite");
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    [Trait("Category", "PostgreSql")]
    public async Task PostgreSqlReadiness_ReportsSelectedProvider()
    {
        var connectionString = Environment.GetEnvironmentVariable("KNOWLEDGE_TEST_POSTGRES");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set KNOWLEDGE_TEST_POSTGRES to an isolated PostgreSQL test database. scripts/verify.sh --integration configures it automatically.");

        await using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["Persistence:Provider"] = "PostgreSql",
            ["Persistence:PostgreSqlConnectionString"] = connectionString,
        });

        var response = await factory.CreateClient().GetAsync("/health/ready", CancellationToken.None);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>(CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(body);
        Assert.Contains(body.Checks, check =>
            check.Name == "persistence" &&
            check.Data.TryGetValue("provider", out var provider) &&
            provider.GetString() == "PostgreSql");
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?> configuration) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(configuration)));
}
