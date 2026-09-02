using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Workspaces.Domain;
using Knowledge.Server.Workspaces.Features;
using Knowledge.Server.Workspaces.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Knowledge.Server.IntegrationTests;

public sealed class LocalWorkspaceInitializerTests
{
    [Fact]
    public async Task FirstAndRepeatedInitialization_ResolveOneStablePersonalWorkspace()
    {
        await using var database = await TestDatabase.CreateAsync();
        var initializer = CreateInitializer();

        await initializer.InitializeAsync(database.Context, CancellationToken.None);
        await initializer.InitializeAsync(database.Context, CancellationToken.None);

        var owner = await database.Context.Users.SingleAsync();
        var workspace = await database.Context.Workspaces.SingleAsync();
        var membership = await database.Context.Memberships.SingleAsync();
        Assert.Equal(LocalWorkspaceContext.OwnerId, owner.Id);
        Assert.Equal(LocalWorkspaceContext.PersonalWorkspaceId, workspace.Id);
        Assert.Equal(owner.Id, workspace.CreatedBy);
        Assert.Equal(MembershipRole.Owner, membership.Role);
        Assert.Equal(workspace.Id, membership.WorkspaceId);
        Assert.Equal(owner.Id, membership.UserId);
    }

    [Fact]
    public async Task FailedMembershipInsert_RollsBackOwnerAndWorkspace()
    {
        await using var database = await TestDatabase.CreateAsync();
        await database.Context.Database.ExecuteSqlRawAsync(
            """
            CREATE TRIGGER RejectLocalMembership
            BEFORE INSERT ON Memberships
            BEGIN
                SELECT RAISE(ABORT, 'simulated membership failure');
            END;
            """);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            CreateInitializer().InitializeAsync(database.Context, CancellationToken.None));

        await using var verification = database.CreateContext();
        Assert.Equal(0, await verification.Users.CountAsync());
        Assert.Equal(0, await verification.Workspaces.CountAsync());
        Assert.Equal(0, await verification.Memberships.CountAsync());
    }

    [Fact]
    public async Task WorkspaceContext_RemainsPersonalWorkspaceWhenAnotherWorkspaceExists()
    {
        await using var database = await TestDatabase.CreateAsync();
        await CreateInitializer().InitializeAsync(database.Context, CancellationToken.None);
        var secondWorkspace = new Workspace(
            Guid.NewGuid(),
            "Other workspace",
            LocalWorkspaceContext.OwnerId,
            DateTimeOffset.UtcNow);
        database.Context.Workspaces.Add(secondWorkspace);
        await database.Context.SaveChangesAsync();

        IWorkspaceContext context = new LocalWorkspaceContext();
        Assert.Equal(LocalWorkspaceContext.PersonalWorkspaceId, context.WorkspaceId);
        Assert.NotEqual(secondWorkspace.Id, context.WorkspaceId);
        Assert.Equal(LocalWorkspaceContext.OwnerId, context.ActorId);
    }

    [Fact]
    public void PostgreSqlComposition_RegistersDeniedFallbackWithoutLocalInitializer()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Persistence:Provider"] = "PostgreSql",
                ["Persistence:PostgreSqlConnectionString"] = "Host=localhost;Database=knowledge",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddPersistence(configuration, Directory.GetCurrentDirectory());

        using var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<IWorkspaceContext>();
        Assert.IsType<UnavailableWorkspaceContext>(context);
        Assert.Throws<WorkspaceAccessDeniedException>(() => context.WorkspaceId);
        Assert.Throws<WorkspaceAccessDeniedException>(() => context.ActorId);
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ImplementationType == typeof(LocalWorkspaceInitializer));
    }

    private static LocalWorkspaceInitializer CreateInitializer() => new(
        null!,
        Options.Create(new LocalWorkspaceOptions()),
        TimeProvider.System,
        NullLogger<LocalWorkspaceInitializer>.Instance);

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<SqliteKnowledgeDbContext> options;

        private TestDatabase(
            SqliteConnection connection,
            DbContextOptions<SqliteKnowledgeDbContext> options,
            SqliteKnowledgeDbContext context)
        {
            this.connection = connection;
            this.options = options;
            Context = context;
        }

        public SqliteKnowledgeDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = ":memory:",
                ForeignKeys = true,
            }.ConnectionString;
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new SqliteKnowledgeDbContext(options);
            await context.Database.MigrateAsync();
            return new TestDatabase(connection, options, context);
        }

        public SqliteKnowledgeDbContext CreateContext() => new(options);

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
