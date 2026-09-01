using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Knowledge.Server.Workspaces.Infrastructure;

public sealed class LocalWorkspaceInitializer(
    IServiceScopeFactory scopeFactory,
    IOptions<LocalWorkspaceOptions> options,
    TimeProvider timeProvider,
    ILogger<LocalWorkspaceInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<SqliteKnowledgeDbContext>();
            await dbContext.Database.MigrateAsync(cancellationToken);
            await InitializeAsync(dbContext, cancellationToken);
            logger.LogInformation(
                "Resolved local owner {OwnerId} and personal workspace {WorkspaceId}.",
                LocalWorkspaceContext.OwnerId,
                LocalWorkspaceContext.PersonalWorkspaceId);
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "Local workspace initialization failed. Verify the SQLite database is writable and the configured local identity does not conflict with existing data.");
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal async Task InitializeAsync(
        SqliteKnowledgeDbContext dbContext,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var owner = await dbContext.Users.FindAsync([LocalWorkspaceContext.OwnerId], cancellationToken);
        var workspace = await dbContext.Workspaces.FindAsync(
            [LocalWorkspaceContext.PersonalWorkspaceId],
            cancellationToken);
        var membership = await dbContext.Memberships.FindAsync(
            [LocalWorkspaceContext.PersonalWorkspaceId, LocalWorkspaceContext.OwnerId],
            cancellationToken);

        ValidateExistingRecords(owner, workspace, membership);

        var createdAt = timeProvider.GetUtcNow();
        owner ??= new User(LocalWorkspaceContext.OwnerId, options.Value.OwnerDisplayName, createdAt);
        workspace ??= new Workspace(
            LocalWorkspaceContext.PersonalWorkspaceId,
            options.Value.WorkspaceName,
            LocalWorkspaceContext.OwnerId,
            createdAt);
        membership ??= new Membership(
            LocalWorkspaceContext.PersonalWorkspaceId,
            LocalWorkspaceContext.OwnerId,
            MembershipRole.Owner,
            createdAt);

        if (dbContext.Entry(owner).State == EntityState.Detached)
        {
            dbContext.Users.Add(owner);
        }

        if (dbContext.Entry(workspace).State == EntityState.Detached)
        {
            dbContext.Workspaces.Add(workspace);
        }

        if (dbContext.Entry(membership).State == EntityState.Detached)
        {
            dbContext.Memberships.Add(membership);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void ValidateExistingRecords(
        User? owner,
        Workspace? workspace,
        Membership? membership)
    {
        if (workspace is not null && workspace.CreatedBy != LocalWorkspaceContext.OwnerId)
        {
            throw new InvalidOperationException(
                "The local personal workspace ID belongs to a workspace created by a different user.");
        }

        if (membership is not null && membership.Role != MembershipRole.Owner)
        {
            throw new InvalidOperationException(
                "The local owner's personal workspace membership does not have the Owner role.");
        }

        if (owner is null && (workspace is not null || membership is not null))
        {
            throw new InvalidOperationException(
                "Local workspace data refers to the configured owner, but that owner record is missing.");
        }
    }
}
