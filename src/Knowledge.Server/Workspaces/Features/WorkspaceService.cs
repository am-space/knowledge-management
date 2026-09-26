using System.Text.Json;
using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.Workspaces.Features;

public sealed class WorkspaceService(
    KnowledgeDbContext dbContext,
    ITrustedActorContext actorContext,
    TimeProvider timeProvider)
{
    public const int DefaultPageSize = 50;
    public const int MaximumPageSize = 100;
    public const int MaximumCursorLength = 2048;

    public async Task<WorkspaceResult> CreateAsync(
        string? name,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId();
        var errors = ValidateName(name);
        if (errors is not null)
        {
            return new WorkspaceResult(WorkspaceResultStatus.ValidationFailed, Errors: errors);
        }

        var now = timeProvider.GetUtcNow();
        var workspace = new Workspace(Guid.NewGuid(), name!, actorId, now);
        var membership = new Membership(workspace.Id, actorId, MembershipRole.Owner, now);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        dbContext.Workspaces.Add(workspace);
        dbContext.Memberships.Add(membership);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            finally
            {
                dbContext.Entry(membership).State = EntityState.Detached;
                dbContext.Entry(workspace).State = EntityState.Detached;
            }

            throw;
        }

        return new WorkspaceResult(WorkspaceResultStatus.Created, ToInfo(workspace));
    }

    public async Task<WorkspacePageResult> ListAsync(
        int pageSize = DefaultPageSize,
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId();
        var defaultWorkspaceId = actorContext.DefaultWorkspaceId;
        if (defaultWorkspaceId == Guid.Empty ||
            !await IsOwnerAsync(defaultWorkspaceId, actorId, cancellationToken))
        {
            throw new WorkspaceAccessDeniedException();
        }

        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (pageSize is < 1 or > MaximumPageSize)
        {
            errors["pageSize"] = ["Page size must be between 1 and 100."];
        }

        CursorState? continuation = null;
        if (cursor is not null &&
            (cursor.Length is 0 or > MaximumCursorLength ||
             !TryReadCursor(cursor, actorId, pageSize, out continuation)))
        {
            errors["cursor"] = ["Cursor is invalid for this workspace list."];
        }

        if (errors.Count != 0)
        {
            return new WorkspacePageResult(Errors: errors);
        }

        var workspaces = (await WorkspaceListing.ListOwnedAsync(
                dbContext,
                actorId,
                pageSize + 1,
                continuation?.CreatedAt,
                continuation?.Id,
                cancellationToken))
            .Select(ToInfo)
            .ToList();

        var hasMore = workspaces.Count > pageSize;
        if (hasMore)
        {
            workspaces.RemoveAt(pageSize);
        }

        var last = workspaces.LastOrDefault();
        var nextCursor = hasMore && last is not null
            ? WriteCursor(new CursorState(1, "workspaces", actorId, pageSize, last.CreatedAt, last.Id))
            : null;

        return new WorkspacePageResult(new WorkspacePage(workspaces, nextCursor, defaultWorkspaceId));
    }

    public async Task<WorkspaceInfo?> GetAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId();
        return await OwnedWorkspaces(actorId)
            .Where(workspace => workspace.Id == workspaceId)
            .Select(workspace => new WorkspaceInfo(
                workspace.Id,
                workspace.Name,
                workspace.CreatedAt,
                workspace.CreatedBy))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<AuthorizedWorkspaceContext?> AuthorizeSelectionAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId();
        return await IsOwnerAsync(workspaceId, actorId, cancellationToken)
            ? new AuthorizedWorkspaceContext(workspaceId, actorId)
            : null;
    }

    public async Task<WorkspaceResult> RenameAsync(
        Guid workspaceId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId();
        var current = await OwnedWorkspaces(actorId)
            .Where(workspace => workspace.Id == workspaceId)
            .Select(workspace => new WorkspaceInfo(
                workspace.Id,
                workspace.Name,
                workspace.CreatedAt,
                workspace.CreatedBy))
            .SingleOrDefaultAsync(cancellationToken);
        if (current is null)
        {
            return new WorkspaceResult(WorkspaceResultStatus.NotFound);
        }

        var errors = ValidateName(name);
        if (errors is not null)
        {
            return new WorkspaceResult(WorkspaceResultStatus.ValidationFailed, Errors: errors);
        }

        var normalizedName = name!.Trim();
        if (current.Name == normalizedName)
        {
            return new WorkspaceResult(WorkspaceResultStatus.Updated, current);
        }

        // The predicate retains tenant scope even if an owner membership changes between reads.
        var updated = await OwnedWorkspaces(actorId)
            .Where(workspace => workspace.Id == workspaceId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(workspace => workspace.Name, normalizedName),
                cancellationToken);
        if (updated == 0)
        {
            return new WorkspaceResult(WorkspaceResultStatus.NotFound);
        }

        return new WorkspaceResult(
            WorkspaceResultStatus.Updated,
            current with { Name = normalizedName });
    }

    private IQueryable<Workspace> OwnedWorkspaces(Guid actorId) =>
        dbContext.Workspaces.AsNoTracking()
            .Where(workspace => dbContext.Memberships.Any(membership =>
                membership.WorkspaceId == workspace.Id &&
                membership.UserId == actorId &&
                membership.Role == MembershipRole.Owner));

    private Task<bool> IsOwnerAsync(Guid workspaceId, Guid actorId, CancellationToken cancellationToken) =>
        dbContext.Memberships.AsNoTracking().AnyAsync(membership =>
            membership.WorkspaceId == workspaceId &&
            membership.UserId == actorId &&
            membership.Role == MembershipRole.Owner,
            cancellationToken);

    private Guid GetActorId()
    {
        var actorId = actorContext.ActorId;
        return actorId != Guid.Empty ? actorId : throw new WorkspaceAccessDeniedException();
    }

    private static Dictionary<string, string[]>? ValidateName(string? name)
    {
        var normalized = name?.Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return new(StringComparer.Ordinal) { ["name"] = ["Workspace name is required."] };
        }

        return normalized.Length > Workspace.MaxNameLength
            ? new(StringComparer.Ordinal)
            {
                ["name"] = [$"Workspace name cannot exceed {Workspace.MaxNameLength} characters."],
            }
            : null;
    }

    private static WorkspaceInfo ToInfo(Workspace workspace) =>
        new(workspace.Id, workspace.Name, workspace.CreatedAt, workspace.CreatedBy);

    private static string WriteCursor(CursorState state) =>
        Convert.ToBase64String(JsonSerializer.SerializeToUtf8Bytes(state))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static bool TryReadCursor(
        string cursor,
        Guid actorId,
        int pageSize,
        out CursorState? state)
    {
        state = null;
        try
        {
            if (cursor.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and '_'))
            {
                return false;
            }

            var base64 = cursor.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight((base64.Length + 3) / 4 * 4, '=');
            state = JsonSerializer.Deserialize<CursorState>(Convert.FromBase64String(base64));
            return state is { Version: 1, Operation: "workspaces" } &&
                state.ActorId == actorId && state.PageSize == pageSize &&
                state.Id != Guid.Empty && state.CreatedAt.Offset == TimeSpan.Zero;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }

    private sealed record CursorState(
        int Version,
        string Operation,
        Guid ActorId,
        int PageSize,
        DateTimeOffset CreatedAt,
        Guid Id);
}

public sealed record WorkspaceInfo(Guid Id, string Name, DateTimeOffset CreatedAt, Guid CreatedBy);

public sealed record WorkspacePage(
    IReadOnlyList<WorkspaceInfo> Items,
    string? NextCursor,
    Guid DefaultWorkspaceId);

public sealed record WorkspacePageResult(
    WorkspacePage? Page = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);

public enum WorkspaceResultStatus { Created, Updated, NotFound, ValidationFailed }

public sealed record WorkspaceResult(
    WorkspaceResultStatus Status,
    WorkspaceInfo? Workspace = null,
    IReadOnlyDictionary<string, string[]>? Errors = null);
