using System.Data.Common;
using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Knowledge.Domain;
using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.IntegrationTests;

public sealed class KnowledgeMigrationTests
{
    [Fact]
    public async Task InitialRevisionFailure_PreservesTrackedInsertsForRetry()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knowledge-retry-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
                .UseSqlite($"Data Source={databasePath};Foreign Keys=True")
                .Options;
            await using var context = new SqliteKnowledgeDbContext(options);
            await context.Database.MigrateAsync();

            var now = DateTimeOffset.UtcNow;
            var owner = new User(Guid.NewGuid(), "Local owner", now);
            var workspace = new Workspace(Guid.NewGuid(), "Personal", owner.Id, now);
            var node = KnowledgeNode.CreateArticle(
                Guid.NewGuid(),
                workspace.Id,
                Guid.NewGuid(),
                "Article",
                "Content",
                owner.Id,
                now);

            context.AddRange(owner, workspace, node);
            await context.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FailCurrentRevisionUpdate
                BEFORE UPDATE OF "CurrentRevisionId" ON "KnowledgeNodes"
                BEGIN
                    SELECT RAISE(ABORT, 'forced second-phase failure');
                END;
                """);

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            Assert.All(
                context.ChangeTracker.Entries().Where(entry =>
                    entry.Entity is User or Workspace or KnowledgeNode or KnowledgeRevision),
                entry => Assert.Equal(EntityState.Added, entry.State));

            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER FailCurrentRevisionUpdate;");
            await context.SaveChangesAsync();

            Assert.Single(await context.KnowledgeNodes.ToListAsync());
            Assert.Single(await context.KnowledgeRevisions.ToListAsync());
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    public async Task Sqlite_EmptyDatabaseMigratesToLatestAndEnforcesModel()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"knowledge-migration-{Guid.NewGuid():N}.db");
        try
        {
            var options = new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
                .UseSqlite($"Data Source={databasePath};Foreign Keys=True")
                .Options;
            await using var context = new SqliteKnowledgeDbContext(options);

            await VerifyMigratedModel(context);
        }
        finally
        {
            File.Delete(databasePath);
        }
    }

    [Fact]
    [Trait("Category", "PostgreSql")]
    public async Task PostgreSql_EmptyDatabaseMigratesToLatestAndEnforcesModel()
    {
        var connectionString = Environment.GetEnvironmentVariable("KNOWLEDGE_TEST_POSTGRES");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set KNOWLEDGE_TEST_POSTGRES to an isolated PostgreSQL test database. scripts/verify.sh --integration configures it automatically.");

        var options = new DbContextOptionsBuilder<PostgreSqlKnowledgeDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new PostgreSqlKnowledgeDbContext(options);
        await context.Database.EnsureDeletedAsync();

        await VerifyMigratedModel(context);
    }

    private static async Task VerifyMigratedModel(KnowledgeDbContext context)
    {
        await context.Database.MigrateAsync();
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync());

        var now = DateTimeOffset.UtcNow;
        var owner = new User(Guid.NewGuid(), "Local owner", now);
        var workspace = new Workspace(Guid.NewGuid(), "Personal", owner.Id, now);
        var otherWorkspace = new Workspace(Guid.NewGuid(), "Other", owner.Id, now);
        var membership = new Membership(workspace.Id, owner.Id, MembershipRole.Owner, now);
        var node = KnowledgeNode.CreateArticle(
            Guid.NewGuid(),
            workspace.Id,
            Guid.NewGuid(),
            "First article",
            "# First article\n",
            owner.Id,
            now);

        context.AddRange(owner, workspace, otherWorkspace, membership, node);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var storedNode = await context.KnowledgeNodes
            .Include(candidate => candidate.CurrentRevision)
            .SingleAsync(candidate => candidate.Id == node.Id);
        Assert.Equal(1, storedNode.CurrentRevision?.Version);
        Assert.Equal(node.CurrentRevisionId, storedNode.CurrentRevisionId);

        context.Entry(storedNode.CurrentRevision!).Property(revision => revision.Title).CurrentValue =
            "Mutated";
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        context.ChangeTracker.Clear();

        await Assert.ThrowsAnyAsync<DbException>(() => context.Database.ExecuteSqlInterpolatedAsync($$"""
            INSERT INTO "KnowledgeRevisions"
                ("Id", "WorkspaceId", "NodeId", "Version", "Title", "ContentMarkdown", "CreatedAt", "CreatedBy")
            VALUES
                ({{Guid.NewGuid()}}, {{otherWorkspace.Id}}, {{node.Id}}, 2, 'Cross workspace', 'Invalid', {{now}}, {{owner.Id}})
            """));

        var otherNode = KnowledgeNode.CreateArticle(
            Guid.NewGuid(),
            workspace.Id,
            Guid.NewGuid(),
            "Other article",
            "Other content",
            owner.Id,
            now);
        context.Add(otherNode);
        await context.SaveChangesAsync();

        await Assert.ThrowsAnyAsync<DbException>(() => context.Database.ExecuteSqlInterpolatedAsync($$"""
            UPDATE "KnowledgeNodes"
            SET "CurrentRevisionId" = {{node.CurrentRevisionId}}
            WHERE "Id" = {{otherNode.Id}}
            """));
    }
}
