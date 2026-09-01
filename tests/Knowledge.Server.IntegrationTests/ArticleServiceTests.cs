using System.Data.Common;
using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Knowledge.Domain;
using Knowledge.Server.Knowledge.Features;
using Knowledge.Server.Workspaces.Domain;
using Knowledge.Server.Workspaces.Features;
using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.IntegrationTests;

public sealed class ArticleServiceTests
{
    [Theory]
    [InlineData(PersistenceProvider.Sqlite)]
    [InlineData(PersistenceProvider.PostgreSql)]
    [Trait("Category", "PostgreSql")]
    public async Task Lifecycle_PreservesHistoryConcurrencyAndWorkspaceIsolation(
        PersistenceProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        var (ownerId, workspaceId, otherWorkspaceId) = await SeedWorkspacesAsync(database.Context);
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero));
        var service = new ArticleService(
            database.Context,
            new StubWorkspaceContext(workspaceId, ownerId),
            clock);

        var created = await service.CreateAsync(" Article ", "# Initial\n");

        Assert.Equal(ArticleResultStatus.Created, created.Status);
        Assert.NotNull(created.Article);
        Assert.Equal(1, created.Article.CurrentRevision.Version);
        Assert.Equal("Article", created.Article.CurrentRevision.Title);

        database.Context.ChangeTracker.Clear();
        var found = await service.GetAsync(created.Article.Id);
        Assert.Equal(created.Article, found.Article);

        clock.Advance(TimeSpan.FromMinutes(1));
        var updated = await service.UpdateAsync(
            created.Article.Id,
            expectedRevisionVersion: 1,
            "Updated",
            "# Updated\n");

        Assert.Equal(ArticleResultStatus.Updated, updated.Status);
        Assert.Equal(2, updated.Article?.CurrentRevision.Version);
        Assert.Equal(2, await database.Context.KnowledgeRevisions
            .CountAsync(revision => revision.NodeId == created.Article.Id));
        Assert.Equal("# Initial\n", await database.Context.KnowledgeRevisions
            .Where(revision => revision.NodeId == created.Article.Id && revision.Version == 1)
            .Select(revision => revision.ContentMarkdown)
            .SingleAsync());

        var stale = await service.UpdateAsync(
            created.Article.Id,
            expectedRevisionVersion: 1,
            "Stale",
            "Stale");
        Assert.Equal(ArticleResultStatus.RevisionConflict, stale.Status);
        Assert.Equal(2, stale.CurrentRevisionVersion);
        Assert.Equal(2, await database.Context.KnowledgeRevisions
            .CountAsync(revision => revision.NodeId == created.Article.Id));

        var invalidActorService = new ArticleService(
            database.Context,
            new StubWorkspaceContext(workspaceId, Guid.NewGuid()),
            clock);
        await Assert.ThrowsAsync<DbUpdateException>(() => invalidActorService.UpdateAsync(
            created.Article.Id,
            expectedRevisionVersion: 2,
            "Invalid actor",
            "Invalid actor"));
        Assert.Equal(2, await database.Context.KnowledgeRevisions
            .CountAsync(revision => revision.NodeId == created.Article.Id));
        Assert.Equal(2, await database.Context.KnowledgeNodes
            .Where(node => node.Id == created.Article.Id)
            .Select(node => node.CurrentRevision!.Version)
            .SingleAsync());

        var otherWorkspaceService = new ArticleService(
            database.Context,
            new StubWorkspaceContext(otherWorkspaceId, ownerId),
            clock);
        Assert.Equal(
            ArticleResultStatus.NotFound,
            (await otherWorkspaceService.GetAsync(created.Article.Id)).Status);
        Assert.Equal(
            ArticleResultStatus.NotFound,
            (await otherWorkspaceService.UpdateAsync(
                created.Article.Id,
                2,
                "Guessed",
                "Guessed")).Status);
        Assert.Equal(2, await database.Context.KnowledgeRevisions
            .CountAsync(revision => revision.NodeId == created.Article.Id));
    }

    [Theory]
    [InlineData(PersistenceProvider.Sqlite)]
    [InlineData(PersistenceProvider.PostgreSql)]
    [Trait("Category", "PostgreSql")]
    public async Task FailedPointerUpdate_RollsBackNewRevision(PersistenceProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        var (ownerId, workspaceId, _) = await SeedWorkspacesAsync(database.Context);
        var service = new ArticleService(
            database.Context,
            new StubWorkspaceContext(workspaceId, ownerId),
            new FixedTimeProvider(DateTimeOffset.UtcNow));
        var created = await service.CreateAsync("Article", "Initial");
        database.Context.ChangeTracker.Clear();
        await database.CreatePointerFailureTriggerAsync();

        await Assert.ThrowsAnyAsync<DbException>(() => service.UpdateAsync(
            created.Article!.Id,
            1,
            "Updated",
            "Updated"));

        database.Context.ChangeTracker.Clear();
        Assert.Equal(1, await database.Context.KnowledgeRevisions
            .CountAsync(revision => revision.NodeId == created.Article!.Id));
        Assert.Equal(1, await database.Context.KnowledgeNodes
            .Where(node => node.Id == created.Article!.Id)
            .Select(node => node.CurrentRevision!.Version)
            .SingleAsync());
    }

    [Fact]
    public async Task ValidationAndCancellation_DoNotWrite()
    {
        await using var database = await TestDatabase.CreateAsync(PersistenceProvider.Sqlite);
        var (ownerId, workspaceId, _) = await SeedWorkspacesAsync(database.Context);
        var service = new ArticleService(
            database.Context,
            new StubWorkspaceContext(workspaceId, ownerId),
            new FixedTimeProvider(DateTimeOffset.UtcNow));

        var invalid = await service.CreateAsync(" ", null);
        Assert.Equal(ArticleResultStatus.ValidationFailed, invalid.Status);
        Assert.Equal(["contentMarkdown", "title"], invalid.Errors!.Keys.Order());
        Assert.Empty(await database.Context.KnowledgeNodes.ToListAsync());

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.GetAsync(Guid.NewGuid(), cancellation.Token));
    }

    private static async Task<(Guid OwnerId, Guid WorkspaceId, Guid OtherWorkspaceId)>
        SeedWorkspacesAsync(KnowledgeDbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var owner = new User(Guid.NewGuid(), "Owner", now);
        var workspace = new Workspace(Guid.NewGuid(), "Primary", owner.Id, now);
        var otherWorkspace = new Workspace(Guid.NewGuid(), "Other", owner.Id, now);
        context.AddRange(
            owner,
            workspace,
            otherWorkspace,
            new Membership(workspace.Id, owner.Id, MembershipRole.Owner, now),
            new Membership(otherWorkspace.Id, owner.Id, MembershipRole.Owner, now));
        await context.SaveChangesAsync();
        return (owner.Id, workspace.Id, otherWorkspace.Id);
    }

    private sealed record StubWorkspaceContext(Guid WorkspaceId, Guid ActorId) : IWorkspaceContext;

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public void Advance(TimeSpan duration) => utcNow += duration;
    }

    private sealed class TestDatabase(KnowledgeDbContext context, string? sqlitePath) : IAsyncDisposable
    {
        public KnowledgeDbContext Context { get; } = context;

        public static async Task<TestDatabase> CreateAsync(PersistenceProvider provider)
        {
            KnowledgeDbContext context;
            string? sqlitePath = null;
            if (provider == PersistenceProvider.Sqlite)
            {
                sqlitePath = Path.Combine(Path.GetTempPath(), $"articles-{Guid.NewGuid():N}.db");
                var options = new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
                    .UseSqlite($"Data Source={sqlitePath};Foreign Keys=True;Default Timeout=30")
                    .Options;
                context = new SqliteKnowledgeDbContext(options);
            }
            else
            {
                var connectionString = Environment.GetEnvironmentVariable("KNOWLEDGE_TEST_POSTGRES");
                Assert.False(string.IsNullOrWhiteSpace(connectionString),
                    "scripts/verify.sh --integration configures PostgreSQL.");
                var options = new DbContextOptionsBuilder<PostgreSqlKnowledgeDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
                context = new PostgreSqlKnowledgeDbContext(options);
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();
            return new TestDatabase(context, sqlitePath);
        }

        public Task CreatePointerFailureTriggerAsync() => Context.Database.IsSqlite()
            ? Context.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailArticlePointerUpdate
                BEFORE UPDATE OF "CurrentRevisionId" ON "KnowledgeNodes"
                BEGIN
                    SELECT RAISE(ABORT, 'forced pointer failure');
                END;
                """)
            : Context.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION fail_article_pointer_update() RETURNS trigger AS $trigger$
                BEGIN
                    RAISE EXCEPTION 'forced pointer failure';
                END;
                $trigger$ LANGUAGE plpgsql;
                CREATE TRIGGER fail_article_pointer_update
                BEFORE UPDATE OF "CurrentRevisionId" ON "KnowledgeNodes"
                FOR EACH ROW EXECUTE FUNCTION fail_article_pointer_update();
                """);

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            if (sqlitePath is not null)
            {
                File.Delete(sqlitePath);
            }
        }
    }
}
