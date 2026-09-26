using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Workspaces.Domain;
using Knowledge.Server.Workspaces.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Knowledge.Server.IntegrationTests;

public sealed class WorkspaceEndpointTests
{
    [Fact]
    public async Task WorkspaceRoutes_CreateListGetRenameAndKeepLegacyDefault()
    {
        await using var factory = new WorkspaceApiFactory();
        using var client = factory.CreateClient();

        var legacyCreate = await client.PostAsJsonAsync("/api/articles", new
        {
            title = "Original",
            contentMarkdown = "# Original\n",
        });
        Assert.Equal(HttpStatusCode.Created, legacyCreate.StatusCode);
        var legacy = await ReadJsonAsync(legacyCreate);

        var createdResponse = await client.PostAsJsonAsync("/api/workspaces", new
        {
            name = " Second ",
            createdBy = Guid.NewGuid(),
            ownerId = Guid.NewGuid(),
        });
        Assert.Equal(HttpStatusCode.Created, createdResponse.StatusCode);
        var created = await ReadJsonAsync(createdResponse);
        var id = created.RootElement.GetProperty("id").GetGuid();
        Assert.Equal("Second", created.RootElement.GetProperty("name").GetString());
        Assert.Equal(LocalWorkspaceContext.OwnerId, created.RootElement.GetProperty("createdBy").GetGuid());
        Assert.Equal($"/api/workspaces/{id:D}", createdResponse.Headers.Location?.OriginalString);
        Assert.Equal(["createdAt", "createdBy", "id", "name"],
            created.RootElement.EnumerateObject().Select(property => property.Name).Order());

        var listed = await ReadJsonAsync(await client.GetAsync("/api/workspaces?pageSize=1"));
        Assert.Equal(LocalWorkspaceContext.PersonalWorkspaceId,
            listed.RootElement.GetProperty("defaultWorkspaceId").GetGuid());
        Assert.Single(listed.RootElement.GetProperty("items").EnumerateArray());
        var cursor = listed.RootElement.GetProperty("nextCursor").GetString();
        Assert.False(string.IsNullOrWhiteSpace(cursor));
        var secondPage = await ReadJsonAsync(await client.GetAsync(
            $"/api/workspaces?pageSize=1&cursor={Uri.EscapeDataString(cursor)}"));
        Assert.Single(secondPage.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, secondPage.RootElement.GetProperty("nextCursor").ValueKind);

        var get = await ReadJsonAsync(await client.GetAsync($"/api/workspaces/{id:D}"));
        Assert.Equal(id, get.RootElement.GetProperty("id").GetGuid());
        var renamedResponse = await client.PutAsJsonAsync($"/api/workspaces/{id:D}", new { name = " Renamed " });
        Assert.Equal(HttpStatusCode.OK, renamedResponse.StatusCode);
        var renamed = await ReadJsonAsync(renamedResponse);
        Assert.Equal("Renamed", renamed.RootElement.GetProperty("name").GetString());
        Assert.Equal(created.RootElement.GetProperty("createdAt").GetString(),
            renamed.RootElement.GetProperty("createdAt").GetString());

        var legacyGet = await ReadJsonAsync(await client.GetAsync(
            $"/api/articles/{legacy.RootElement.GetProperty("id").GetGuid():D}"));
        Assert.Equal(legacy.RootElement.GetRawText(), legacyGet.RootElement.GetRawText());
    }

    [Fact]
    public async Task WorkspaceRoutes_DenyNonownersWithoutDisclosingWorkspace()
    {
        await using var factory = new WorkspaceApiFactory();
        using var client = factory.CreateClient();
        _ = await client.GetAsync("/api/workspaces");

        var foreignId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            var otherId = Guid.NewGuid();
            var now = DateTimeOffset.UtcNow;
            db.Users.Add(new User(otherId, "Other", now));
            db.Workspaces.AddRange(
                new Workspace(foreignId, "Foreign name", otherId, now),
                new Workspace(viewerId, "Viewer only", LocalWorkspaceContext.OwnerId, now));
            db.Memberships.AddRange(
                new Membership(foreignId, otherId, MembershipRole.Owner, now),
                new Membership(foreignId, LocalWorkspaceContext.OwnerId, MembershipRole.Editor, now),
                new Membership(viewerId, LocalWorkspaceContext.OwnerId, MembershipRole.Viewer, now));
            await db.SaveChangesAsync();
        }

        var absentId = Guid.NewGuid();
        foreach (var id in new[] { foreignId, viewerId, absentId })
        {
            var get = await AssertProblemAsync(await client.GetAsync($"/api/workspaces/{id:D}"),
                HttpStatusCode.NotFound, "urn:knowledge:problem:workspace-not-found");
            Assert.DoesNotContain("Foreign name", get.RootElement.GetRawText(), StringComparison.Ordinal);
            await AssertProblemAsync(await client.PutAsJsonAsync(
                    $"/api/workspaces/{id:D}", new { name = "Attempt" }),
                HttpStatusCode.NotFound, "urn:knowledge:problem:workspace-not-found");
        }

        var listed = await ReadJsonAsync(await client.GetAsync("/api/workspaces"));
        Assert.Single(listed.RootElement.GetProperty("items").EnumerateArray());
    }

    [Fact]
    public async Task WorkspaceRoutes_ValidateNameIdsAndPagination()
    {
        await using var factory = new WorkspaceApiFactory();
        using var client = factory.CreateClient();

        var invalidName = await AssertProblemAsync(
            await client.PostAsJsonAsync("/api/workspaces", new { name = " " }),
            HttpStatusCode.BadRequest, "urn:knowledge:problem:validation");
        Assert.True(invalidName.RootElement.GetProperty("errors").TryGetProperty("name", out _));
        foreach (string? name in new[] { null, new string('x', Workspace.MaxNameLength + 1) })
        {
            var problem = await AssertProblemAsync(
                await client.PostAsJsonAsync("/api/workspaces", new { name }),
                HttpStatusCode.BadRequest, "urn:knowledge:problem:validation");
            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty("name", out _));
        }
        var invalidId = await AssertProblemAsync(await client.GetAsync("/api/workspaces/invalid"),
            HttpStatusCode.BadRequest, "urn:knowledge:problem:validation");
        Assert.True(invalidId.RootElement.GetProperty("errors").TryGetProperty("workspaceId", out _));
        foreach (var query in new[] { "pageSize=0", "pageSize=101", "pageSize=x",
                     "pageSize=1&pageSize=2", "cursor=", "cursor=x", "cursor=x&cursor=y" })
        {
            var problem = await AssertProblemAsync(await client.GetAsync($"/api/workspaces?{query}"),
                HttpStatusCode.BadRequest, "urn:knowledge:problem:validation");
            Assert.True(problem.RootElement.GetProperty("errors").TryGetProperty(
                query.StartsWith("cursor", StringComparison.Ordinal) ? "cursor" : "pageSize", out _));
        }
    }

    [Fact]
    public async Task WorkspaceList_DeniesMissingDefaultMembership_ButExplicitOwnedGetWorks()
    {
        await using var factory = new WorkspaceApiFactory();
        using var client = factory.CreateClient();
        var created = await ReadJsonAsync(await client.PostAsJsonAsync(
            "/api/workspaces", new { name = "Second" }));
        var secondId = created.RootElement.GetProperty("id").GetGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
            db.Memberships.Remove(await db.Memberships.SingleAsync(membership =>
                membership.WorkspaceId == LocalWorkspaceContext.PersonalWorkspaceId &&
                membership.UserId == LocalWorkspaceContext.OwnerId));
            await db.SaveChangesAsync();
        }

        await AssertProblemAsync(await client.GetAsync("/api/workspaces"),
            HttpStatusCode.Forbidden, "urn:knowledge:problem:workspace-access-denied");
        var explicitGet = await client.GetAsync($"/api/workspaces/{secondId:D}");
        Assert.Equal(HttpStatusCode.OK, explicitGet.StatusCode);
    }

    [Fact]
    public async Task HostedProfileWithoutTrustedActor_DeniesWorkspaceRoutes()
    {
        await using var factory = new DeniedWorkspaceApiFactory();
        using var client = factory.CreateClient();
        foreach (var response in new[]
        {
            await client.GetAsync("/api/workspaces"),
            await client.PostAsJsonAsync("/api/workspaces", new { name = "Denied" }),
            await client.GetAsync($"/api/workspaces/{Guid.NewGuid():D}"),
        })
        {
            var problem = await AssertProblemAsync(response, HttpStatusCode.Forbidden,
                "urn:knowledge:problem:workspace-access-denied");
            Assert.Equal("A trusted actor is required.", problem.RootElement.GetProperty("title").GetString());
        }
    }

    private static async Task<JsonDocument> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string type)
    {
        Assert.Equal(status, response.StatusCode);
        var document = await ReadJsonAsync(response);
        Assert.Equal(type, document.RootElement.GetProperty("type").GetString());
        Assert.Equal((int)status, document.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(document.RootElement.GetProperty("traceId").GetString()));
        return document;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private sealed class WorkspaceApiFactory : WebApplicationFactory<Program>
    {
        private readonly string databasePath = Path.Combine(
            Path.GetTempPath(), $"knowledge-workspace-api-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Provider"] = "Sqlite",
                    ["Persistence:SqliteConnectionString"] = $"Data Source={databasePath}",
                }));
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            File.Delete(databasePath);
            File.Delete($"{databasePath}-shm");
            File.Delete($"{databasePath}-wal");
            GC.SuppressFinalize(this);
        }
    }

    private sealed class DeniedWorkspaceApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Persistence:Provider", "PostgreSql");
            builder.UseSetting("Persistence:PostgreSqlConnectionString",
                "Host=127.0.0.1;Port=1;Database=unavailable;Username=none;Password=none;Timeout=1");
        }
    }
}
