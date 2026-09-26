using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Workspaces.Domain;
using Knowledge.Server.Workspaces.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Knowledge.Server.IntegrationTests;

public sealed class WorkspaceServiceTests
{
    [Theory]
    [InlineData(PersistenceProvider.Sqlite)]
    [InlineData(PersistenceProvider.PostgreSql)]
    [Trait("Category", "PostgreSql")]
    public async Task OwnerOperations_AreScopedAndPaginated(PersistenceProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        var ownerId = Guid.NewGuid();
        var otherOwnerId = Guid.NewGuid();
        var defaultId = Guid.NewGuid();
        var foreignId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        database.Context.Users.AddRange(
            new User(ownerId, "Owner", now),
            new User(otherOwnerId, "Other owner", now));
        database.Context.Workspaces.AddRange(
            new Workspace(defaultId, "Personal", ownerId, now),
            new Workspace(foreignId, "Foreign", otherOwnerId, now),
            new Workspace(viewerId, "Viewer only", ownerId, now));
        database.Context.Memberships.AddRange(
            new Membership(defaultId, ownerId, MembershipRole.Owner, now),
            new Membership(foreignId, otherOwnerId, MembershipRole.Owner, now),
            new Membership(foreignId, ownerId, MembershipRole.Editor, now),
            new Membership(viewerId, ownerId, MembershipRole.Viewer, now));
        await database.Context.SaveChangesAsync();

        var actor = new TestActorContext(ownerId, defaultId);
        var service = new WorkspaceService(database.Context, actor, new FixedTimeProvider(now));
        Assert.Null(await service.GetAsync(foreignId));
        Assert.Null(await service.AuthorizeSelectionAsync(foreignId));
        Assert.Null(await service.GetAsync(viewerId));
        Assert.Null(await service.AuthorizeSelectionAsync(viewerId));
        Assert.Equal(WorkspaceResultStatus.NotFound,
            (await service.RenameAsync(foreignId, "Stolen")).Status);
        Assert.Equal(WorkspaceResultStatus.NotFound,
            (await service.RenameAsync(viewerId, "Stolen")).Status);

        var createdIds = new List<Guid>();
        for (var index = 0; index < 101; index++)
        {
            var created = await service.CreateAsync(" Same name ");
            Assert.Equal(WorkspaceResultStatus.Created, created.Status);
            Assert.Equal("Same name", created.Workspace!.Name);
            createdIds.Add(created.Workspace.Id);
        }

        var seen = new List<Guid>();
        string? cursor = null;
        do
        {
            var page = await service.ListAsync(17, cursor);
            Assert.Null(page.Errors);
            Assert.Equal(defaultId, page.Page!.DefaultWorkspaceId);
            Assert.InRange(page.Page.Items.Count, 1, 17);
            seen.AddRange(page.Page.Items.Select(item => item.Id));
            cursor = page.Page.NextCursor;
        } while (cursor is not null);

        Assert.Equal(102, seen.Count);
        Assert.Equal(102, seen.Distinct().Count());
        Assert.DoesNotContain(foreignId, seen);
        Assert.DoesNotContain(viewerId, seen);
        Assert.All(createdIds, id => Assert.Contains(id, seen));

        var renamed = await service.RenameAsync(createdIds[0], " Renamed ");
        Assert.Equal(WorkspaceResultStatus.Updated, renamed.Status);
        Assert.Equal("Renamed", renamed.Workspace!.Name);
        Assert.Equal(ownerId, renamed.Workspace.CreatedBy);
        Assert.NotNull(await service.AuthorizeSelectionAsync(createdIds[0]));
        Assert.Equal("Renamed", (await service.GetAsync(createdIds[0]))!.Name);
        Assert.Equal(MembershipRole.Owner, await database.Context.Memberships
            .Where(membership => membership.WorkspaceId == createdIds[0])
            .Select(membership => membership.Role)
            .SingleAsync());

        var firstPage = await service.ListAsync(1);
        Assert.NotNull(firstPage.Page!.NextCursor);
        Assert.NotNull((await service.ListAsync(2, firstPage.Page.NextCursor)).Errors?["cursor"]);
        var otherService = new WorkspaceService(
            database.Context,
            new TestActorContext(otherOwnerId, foreignId),
            new FixedTimeProvider(now));
        Assert.NotNull((await otherService.ListAsync(1, firstPage.Page.NextCursor)).Errors?["cursor"]);

        await using var firstRequestContext = database.CreateContext();
        await using var secondRequestContext = database.CreateContext();
        var selected = await Task.WhenAll(
            new WorkspaceService(firstRequestContext, actor, new FixedTimeProvider(now))
                .AuthorizeSelectionAsync(defaultId),
            new WorkspaceService(secondRequestContext, actor, new FixedTimeProvider(now))
                .AuthorizeSelectionAsync(createdIds[0]));
        Assert.Equal(defaultId, selected[0]!.WorkspaceId);
        Assert.Equal(createdIds[0], selected[1]!.WorkspaceId);
        Assert.Equal(ownerId, selected[0]!.ActorId);
        Assert.Equal(ownerId, selected[1]!.ActorId);

        database.Context.Memberships.Remove(await database.Context.Memberships.SingleAsync(
            membership => membership.WorkspaceId == defaultId && membership.UserId == ownerId));
        await database.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<WorkspaceAccessDeniedException>(() => service.ListAsync());
        Assert.NotNull(await service.GetAsync(createdIds[0]));
    }

    [Theory]
    [InlineData(PersistenceProvider.Sqlite)]
    [InlineData(PersistenceProvider.PostgreSql)]
    [Trait("Category", "PostgreSql")]
    public async Task FailedOwnerMembershipInsert_RollsBackWorkspace(PersistenceProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        var ownerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        database.Context.Users.Add(new User(ownerId, "Owner", now));
        await database.Context.SaveChangesAsync();
        await database.CreateMembershipFailureTriggerAsync();

        var service = new WorkspaceService(
            database.Context,
            new TestActorContext(ownerId, Guid.NewGuid()),
            new FixedTimeProvider(now));
        await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync("Failed"));
        Assert.Empty(await database.Context.Workspaces.ToListAsync());
        Assert.Empty(await database.Context.Memberships.ToListAsync());

        await database.DropMembershipFailureTriggerAsync();
        Assert.Equal(WorkspaceResultStatus.Created, (await service.CreateAsync("Succeeded")).Status);
        Assert.Equal(["Succeeded"], await database.Context.Workspaces
            .Select(workspace => workspace.Name).ToListAsync());
    }

    [Theory]
    [InlineData(PersistenceProvider.Sqlite)]
    [InlineData(PersistenceProvider.PostgreSql)]
    [Trait("Category", "PostgreSql")]
    public async Task CancellationBeforeCommit_LeavesNoWorkspaceOrMembership(PersistenceProvider provider)
    {
        await using var database = await TestDatabase.CreateAsync(provider);
        var ownerId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);
        database.Context.Users.Add(new User(ownerId, "Owner", now));
        await database.Context.SaveChangesAsync();

        using var cancellation = new CancellationTokenSource();
        await using var createContext = database.CreateContext(new CancelDuringSaveInterceptor(cancellation));
        var service = new WorkspaceService(
            createContext,
            new TestActorContext(ownerId, Guid.NewGuid()),
            new FixedTimeProvider(now));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CreateAsync("Cancelled", cancellation.Token));

        Assert.Empty(await database.Context.Workspaces.ToListAsync());
        Assert.Empty(await database.Context.Memberships.ToListAsync());
    }

    private sealed record TestActorContext(Guid ActorId, Guid DefaultWorkspaceId)
        : ITrustedActorContext;

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class CancelDuringSaveInterceptor(CancellationTokenSource cancellation)
        : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            cancellation.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            return ValueTask.FromResult(result);
        }
    }

    private sealed class TestDatabase(
        KnowledgeDbContext context,
        PersistenceProvider provider,
        string connectionString,
        string? sqlitePath) : IAsyncDisposable
    {
        public KnowledgeDbContext Context { get; } = context;

        public KnowledgeDbContext CreateContext(params IInterceptor[] interceptors) => provider == PersistenceProvider.Sqlite
            ? new SqliteKnowledgeDbContext(new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
                .UseSqlite(connectionString).AddInterceptors(interceptors).Options)
            : new PostgreSqlKnowledgeDbContext(new DbContextOptionsBuilder<PostgreSqlKnowledgeDbContext>()
                .UseNpgsql(connectionString).AddInterceptors(interceptors).Options);

        public Task CreateMembershipFailureTriggerAsync() => Context.Database.IsSqlite()
            ? Context.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER RejectWorkspaceMembership
                BEFORE INSERT ON Memberships
                BEGIN
                    SELECT RAISE(ABORT, 'simulated membership failure');
                END;
                """)
            : Context.Database.ExecuteSqlRawAsync("""
                CREATE FUNCTION reject_workspace_membership() RETURNS trigger AS $trigger$
                BEGIN
                    RAISE EXCEPTION 'simulated membership failure';
                END;
                $trigger$ LANGUAGE plpgsql;
                CREATE TRIGGER reject_workspace_membership
                BEFORE INSERT ON "Memberships"
                FOR EACH ROW EXECUTE FUNCTION reject_workspace_membership();
                """);

        public Task DropMembershipFailureTriggerAsync() => Context.Database.IsSqlite()
            ? Context.Database.ExecuteSqlRawAsync("DROP TRIGGER RejectWorkspaceMembership;")
            : Context.Database.ExecuteSqlRawAsync("""
                DROP TRIGGER reject_workspace_membership ON "Memberships";
                DROP FUNCTION reject_workspace_membership();
                """);

        public static async Task<TestDatabase> CreateAsync(PersistenceProvider provider)
        {
            KnowledgeDbContext context;
            string? sqlitePath = null;
            string connectionString;
            if (provider == PersistenceProvider.Sqlite)
            {
                sqlitePath = Path.Combine(Path.GetTempPath(), $"workspaces-{Guid.NewGuid():N}.db");
                connectionString = $"Data Source={sqlitePath};Foreign Keys=True";
                var options = new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
                    .UseSqlite(connectionString)
                    .Options;
                context = new SqliteKnowledgeDbContext(options);
            }
            else
            {
                connectionString = Environment.GetEnvironmentVariable("KNOWLEDGE_TEST_POSTGRES")!;
                Assert.False(string.IsNullOrWhiteSpace(connectionString),
                    "scripts/verify.sh --integration configures PostgreSQL.");
                var options = new DbContextOptionsBuilder<PostgreSqlKnowledgeDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;
                context = new PostgreSqlKnowledgeDbContext(options);
                await context.Database.EnsureDeletedAsync();
            }

            await context.Database.MigrateAsync();
            return new TestDatabase(context, provider, connectionString, sqlitePath);
        }

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
