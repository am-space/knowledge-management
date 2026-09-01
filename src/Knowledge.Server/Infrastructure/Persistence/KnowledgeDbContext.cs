using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Knowledge.Server.Knowledge.Domain;
using Knowledge.Server.Workspaces.Domain;

namespace Knowledge.Server.Infrastructure.Persistence;

public abstract class KnowledgeDbContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Membership> Memberships => Set<Membership>();

    public DbSet<KnowledgeNode> KnowledgeNodes => Set<KnowledgeNode>();

    public DbSet<KnowledgeRevision> KnowledgeRevisions => Set<KnowledgeRevision>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KnowledgeDbContext).Assembly);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        RejectRevisionChanges();
        var saveState = PrepareInitialRevisionSave();
        if (saveState is null)
        {
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        var ownsTransaction = Database.CurrentTransaction is null;
        using var transaction = ownsTransaction ? Database.BeginTransaction() : null;
        try
        {
            var affectedRows = base.SaveChanges(acceptAllChangesOnSuccess: false);
            PrepareCurrentRevisionPointerSave(saveState);
            affectedRows += base.SaveChanges(acceptAllChangesOnSuccess: false);
            transaction?.Commit();

            if (acceptAllChangesOnSuccess)
            {
                ChangeTracker.AcceptAllChanges();
            }
            else
            {
                RestorePendingStates(saveState);
            }

            return affectedRows;
        }
        catch
        {
            transaction?.Rollback();
            RestorePendingStates(saveState);
            throw;
        }
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        RejectRevisionChanges();
        return SaveChangesWithInitialRevisionsAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void RejectRevisionChanges()
    {
        if (ChangeTracker.Entries<KnowledgeRevision>().Any(entry =>
                entry.State is EntityState.Modified or EntityState.Deleted))
        {
            throw new InvalidOperationException("Knowledge revisions are immutable.");
        }
    }

    private async Task<int> SaveChangesWithInitialRevisionsAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken)
    {
        var saveState = PrepareInitialRevisionSave();
        if (saveState is null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        var ownsTransaction = Database.CurrentTransaction is null;
        await using var transaction = ownsTransaction
            ? await Database.BeginTransactionAsync(cancellationToken)
            : null;
        try
        {
            var affectedRows = await base.SaveChangesAsync(
                acceptAllChangesOnSuccess: false,
                cancellationToken);
            PrepareCurrentRevisionPointerSave(saveState);
            affectedRows += await base.SaveChangesAsync(
                acceptAllChangesOnSuccess: false,
                cancellationToken);
            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            if (acceptAllChangesOnSuccess)
            {
                ChangeTracker.AcceptAllChanges();
            }
            else
            {
                RestorePendingStates(saveState);
            }

            return affectedRows;
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            RestorePendingStates(saveState);
            throw;
        }
    }

    private InitialRevisionSaveState? PrepareInitialRevisionSave()
    {
        ChangeTracker.DetectChanges();
        var initialRevisions = ChangeTracker.Entries<KnowledgeNode>()
            .Where(entry => entry.State == EntityState.Added && entry.Entity.CurrentRevision is not null)
            .Select(entry => (entry.Entity, entry.Entity.CurrentRevision!))
            .ToList();
        if (initialRevisions.Count == 0)
        {
            return null;
        }

        var pendingStates = ChangeTracker.Entries()
            .Where(entry => entry.State != EntityState.Unchanged)
            .ToDictionary(entry => entry, entry => entry.State);

        foreach (var (node, _) in initialRevisions)
        {
            Entry(node).Reference(candidate => candidate.CurrentRevision).CurrentValue = null;
            Entry(node).Property(candidate => candidate.CurrentRevisionId).CurrentValue = null;
        }

        return new InitialRevisionSaveState(initialRevisions, pendingStates);
    }

    private void PrepareCurrentRevisionPointerSave(InitialRevisionSaveState saveState)
    {
        foreach (var entry in saveState.PendingStates.Keys)
        {
            entry.State = EntityState.Unchanged;
        }

        RestoreCurrentRevisionPointers(saveState.InitialRevisions);
    }

    private void RestorePendingStates(InitialRevisionSaveState saveState)
    {
        RestoreCurrentRevisionPointers(saveState.InitialRevisions);
        foreach (var (entry, state) in saveState.PendingStates)
        {
            entry.State = state;
        }
    }

    private void RestoreCurrentRevisionPointers(
        IEnumerable<(KnowledgeNode Node, KnowledgeRevision Revision)> revisions)
    {
        foreach (var (node, revision) in revisions)
        {
            Entry(node).Reference(candidate => candidate.CurrentRevision).CurrentValue = revision;
            Entry(node).Property(candidate => candidate.CurrentRevisionId).CurrentValue = revision.Id;
        }
    }

    private sealed record InitialRevisionSaveState(
        IReadOnlyList<(KnowledgeNode Node, KnowledgeRevision Revision)> InitialRevisions,
        IReadOnlyDictionary<EntityEntry, EntityState> PendingStates);
}
