using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Workspaces.Features;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Knowledge.Server.IntegrationTests;

public sealed class ArticleEndpointTests
{
    [Fact]
    public async Task CreateReadAndUpdate_UseStableArticleContract()
    {
        await using var factory = new ArticleApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/articles",
            new { title = "First article", contentMarkdown = "# First article\n" });
        var created = await ReadJsonAsync(createResponse);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.Equal("article", created.RootElement.GetProperty("type").GetString());
        Assert.Equal(1, CurrentRevision(created).GetProperty("version").GetInt32());
        Assert.Equal("# First article\n", CurrentRevision(created).GetProperty("contentMarkdown").GetString());
        Assert.EndsWith(
            created.RootElement.GetProperty("id").GetGuid().ToString("D"),
            createResponse.Headers.Location?.OriginalString,
            StringComparison.Ordinal);
        AssertUtcTimestamp(created.RootElement.GetProperty("createdAt").GetString());
        AssertUtcTimestamp(CurrentRevision(created).GetProperty("createdAt").GetString());

        var id = created.RootElement.GetProperty("id").GetGuid();
        var getResponse = await client.GetAsync($"/api/articles/{id:D}");
        var found = await ReadJsonAsync(getResponse);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(created.RootElement.GetRawText(), found.RootElement.GetRawText());

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/articles/{id:D}",
            new
            {
                expectedRevisionVersion = 1,
                title = "Updated article",
                contentMarkdown = "# Updated\n",
            });
        var updated = await ReadJsonAsync(updateResponse);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(id, updated.RootElement.GetProperty("id").GetGuid());
        Assert.Equal(2, CurrentRevision(updated).GetProperty("version").GetInt32());
        Assert.Equal("Updated article", CurrentRevision(updated).GetProperty("title").GetString());
        Assert.NotEqual(
            CurrentRevision(created).GetProperty("id").GetGuid(),
            CurrentRevision(updated).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task ValidationAndMalformedInput_UseStableProblemDetails()
    {
        await using var factory = new ArticleApiFactory();
        using var client = factory.CreateClient();

        var validationResponse = await client.PostAsJsonAsync(
            "/api/articles",
            new { title = " ", contentMarkdown = (string?)null });
        var validation = await AssertProblemAsync(
            validationResponse,
            HttpStatusCode.BadRequest,
            "urn:knowledge:problem:validation");
        var errors = validation.RootElement.GetProperty("errors");
        Assert.True(errors.TryGetProperty("title", out _));
        Assert.True(errors.TryGetProperty("contentMarkdown", out _));

        using var malformedContent = new StringContent(
            "{ broken",
            Encoding.UTF8,
            "application/json");
        var malformedResponse = await client.PostAsync("/api/articles", malformedContent);
        var malformed = await AssertProblemAsync(
            malformedResponse,
            HttpStatusCode.BadRequest,
            "urn:knowledge:problem:validation");
        Assert.True(malformed.RootElement.GetProperty("errors").TryGetProperty("request", out _));

        var invalidIdResponse = await client.GetAsync("/api/articles/not-a-uuid");
        var invalidId = await AssertProblemAsync(
            invalidIdResponse,
            HttpStatusCode.BadRequest,
            "urn:knowledge:problem:validation");
        Assert.True(invalidId.RootElement.GetProperty("errors").TryGetProperty("id", out _));

        var nonCanonicalIdResponse = await client.GetAsync(
            $"/api/articles/{Guid.NewGuid().ToString("D").ToUpperInvariant()}");
        await AssertProblemAsync(
            nonCanonicalIdResponse,
            HttpStatusCode.BadRequest,
            "urn:knowledge:problem:validation");
    }

    [Fact]
    public async Task MissingAndStaleArticles_UseDocumentedProblems()
    {
        await using var factory = new ArticleApiFactory();
        using var client = factory.CreateClient();

        var missingResponse = await client.GetAsync($"/api/articles/{Guid.NewGuid():D}");
        await AssertProblemAsync(
            missingResponse,
            HttpStatusCode.NotFound,
            "urn:knowledge:problem:article-not-found");

        var createResponse = await client.PostAsJsonAsync(
            "/api/articles",
            new { title = "Article", contentMarkdown = "Initial" });
        var created = await ReadJsonAsync(createResponse);
        var id = created.RootElement.GetProperty("id").GetGuid();

        var firstUpdate = await client.PutAsJsonAsync(
            $"/api/articles/{id:D}",
            new { expectedRevisionVersion = 1, title = "First edit", contentMarkdown = "First" });
        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);

        var staleUpdate = await client.PutAsJsonAsync(
            $"/api/articles/{id:D}",
            new { expectedRevisionVersion = 1, title = "Stale edit", contentMarkdown = "Stale" });
        var conflict = await AssertProblemAsync(
            staleUpdate,
            HttpStatusCode.Conflict,
            "urn:knowledge:problem:revision-conflict");
        Assert.Equal(2, conflict.RootElement.GetProperty("currentRevisionVersion").GetInt32());
        Assert.False(conflict.RootElement.TryGetProperty("contentMarkdown", out _));
    }

    [Fact]
    public async Task AnotherWorkspace_CannotDiscoverOrUpdateArticle()
    {
        await using var localFactory = new ArticleApiFactory();
        using var localClient = localFactory.CreateClient();
        var createResponse = await localClient.PostAsJsonAsync(
            "/api/articles",
            new { title = "Private", contentMarkdown = "Secret" });
        var created = await ReadJsonAsync(createResponse);
        var id = created.RootElement.GetProperty("id").GetGuid();

        await using var otherFactory = new ArticleApiFactory(
            new FixedWorkspaceContext(Guid.NewGuid(), Guid.NewGuid()),
            localFactory.DatabasePath);
        using var otherClient = otherFactory.CreateClient();

        var readResponse = await otherClient.GetAsync($"/api/articles/{id:D}");
        await AssertProblemAsync(
            readResponse,
            HttpStatusCode.NotFound,
            "urn:knowledge:problem:article-not-found");

        var updateResponse = await otherClient.PutAsJsonAsync(
            $"/api/articles/{id:D}",
            new { expectedRevisionVersion = 1, title = "Changed", contentMarkdown = "Leaked" });
        await AssertProblemAsync(
            updateResponse,
            HttpStatusCode.NotFound,
            "urn:knowledge:problem:article-not-found");
    }

    [Fact]
    public async Task UnresolvedTrustedWorkspace_ReturnsAccessDeniedProblem()
    {
        await using var factory = new ArticleApiFactory(new DeniedWorkspaceContext());
        using var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/articles/{Guid.NewGuid():D}");

        await AssertProblemAsync(
            response,
            HttpStatusCode.Forbidden,
            "urn:knowledge:problem:workspace-access-denied");
    }

    [Fact]
    public async Task CancelledRequest_ReachesPersistenceAndDoesNotPersistArticle()
    {
        await using var factory = new ArticleApiFactory();
        using var client = factory.CreateClient();
        var probe = factory.Services.GetRequiredService<CancellationProbeInterceptor>();
        probe.BlockNextSave = true;
        using var cancellation = new CancellationTokenSource();
        var request = client.PostAsJsonAsync(
            "/api/articles",
            new { title = "Cancelled", contentMarkdown = "Must not persist" },
            cancellation.Token);

        await probe.SaveEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        Assert.Empty(await dbContext.KnowledgeNodes.AsNoTracking().ToListAsync());
        Assert.Empty(await dbContext.KnowledgeRevisions.AsNoTracking().ToListAsync());
    }

    private static JsonElement CurrentRevision(JsonDocument document) =>
        document.RootElement.GetProperty("currentRevision");

    private static async Task<JsonDocument> AssertProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode status,
        string type)
    {
        var problem = await ReadJsonAsync(response);
        Assert.Equal(status, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(type, problem.RootElement.GetProperty("type").GetString());
        Assert.Equal((int)status, problem.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("title").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("traceId").GetString()));
        return problem;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());

    private static void AssertUtcTimestamp(string? value)
    {
        Assert.NotNull(value);
        Assert.EndsWith("Z", value, StringComparison.Ordinal);
        Assert.True(DateTimeOffset.TryParse(value, out _));
    }

    private sealed class ArticleApiFactory : WebApplicationFactory<Program>
    {
        private readonly IWorkspaceContext? workspaceContext;
        private readonly bool ownsDatabase;

        public ArticleApiFactory(IWorkspaceContext? workspaceContext = null, string? databasePath = null)
        {
            this.workspaceContext = workspaceContext;
            ownsDatabase = databasePath is null;
            DatabasePath = databasePath ?? Path.Combine(
                Path.GetTempPath(),
                $"knowledge-article-api-{Guid.NewGuid():N}.db");
        }

        public string DatabasePath { get; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Persistence:Provider"] = "Sqlite",
                    ["Persistence:SqliteConnectionString"] = $"Data Source={DatabasePath}",
                }));

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<CancellationProbeInterceptor>();
                services.AddDbContext<SqliteKnowledgeDbContext>((serviceProvider, options) =>
                    options.AddInterceptors(
                        serviceProvider.GetRequiredService<CancellationProbeInterceptor>()));

                if (workspaceContext is not null)
                {
                    services.RemoveAll<IWorkspaceContext>();
                    services.AddScoped(_ => workspaceContext);
                }
            });
        }

        public override async ValueTask DisposeAsync()
        {
            await base.DisposeAsync();
            if (ownsDatabase)
            {
                File.Delete(DatabasePath);
                File.Delete($"{DatabasePath}-shm");
                File.Delete($"{DatabasePath}-wal");
            }

            GC.SuppressFinalize(this);
        }
    }

    private sealed record FixedWorkspaceContext(Guid WorkspaceId, Guid ActorId) : IWorkspaceContext;

    private sealed class DeniedWorkspaceContext : IWorkspaceContext
    {
        public Guid WorkspaceId => throw new WorkspaceAccessDeniedException();

        public Guid ActorId => throw new WorkspaceAccessDeniedException();
    }

    private sealed class CancellationProbeInterceptor : SaveChangesInterceptor
    {
        public TaskCompletionSource SaveEntered { get; } = new(
            TaskCreationOptions.RunContinuationsAsynchronously);

        public bool BlockNextSave { get; set; }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (BlockNextSave)
            {
                BlockNextSave = false;
                SaveEntered.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            return result;
        }
    }
}
