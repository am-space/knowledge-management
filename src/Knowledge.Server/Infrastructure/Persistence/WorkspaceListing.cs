using Knowledge.Server.Workspaces.Domain;
using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.Infrastructure.Persistence;

internal static class WorkspaceListing
{
    public static async Task<List<Workspace>> ListOwnedAsync(
        KnowledgeDbContext dbContext,
        Guid actorId,
        int take,
        DateTimeOffset? afterCreatedAt,
        Guid? afterId,
        CancellationToken cancellationToken)
    {
        if (dbContext.Database.IsSqlite())
        {
            // SQLite stores UTC DateTimeOffset values as sortable text but EF cannot translate
            // ordering or comparisons for that CLR type. Keep this provider query in Infrastructure.
            var role = nameof(MembershipRole.Owner);
            return afterCreatedAt is null
                ? await dbContext.Workspaces.FromSqlInterpolated($"""
                    SELECT w.* FROM "Workspaces" AS w
                    WHERE EXISTS (
                        SELECT 1 FROM "Memberships" AS m
                        WHERE m."WorkspaceId" = w."Id"
                          AND m."UserId" = {actorId}
                          AND m."Role" = {role})
                    ORDER BY w."CreatedAt", w."Id"
                    LIMIT {take}
                    """).AsNoTracking().ToListAsync(cancellationToken)
                : await dbContext.Workspaces.FromSqlInterpolated($"""
                    SELECT w.* FROM "Workspaces" AS w
                    WHERE EXISTS (
                        SELECT 1 FROM "Memberships" AS m
                        WHERE m."WorkspaceId" = w."Id"
                          AND m."UserId" = {actorId}
                          AND m."Role" = {role})
                      AND (w."CreatedAt" > {afterCreatedAt.Value}
                           OR (w."CreatedAt" = {afterCreatedAt.Value} AND w."Id" > {afterId!.Value}))
                    ORDER BY w."CreatedAt", w."Id"
                    LIMIT {take}
                    """).AsNoTracking().ToListAsync(cancellationToken);
        }

        var query = dbContext.Workspaces.AsNoTracking()
            .Where(workspace => dbContext.Memberships.Any(membership =>
                membership.WorkspaceId == workspace.Id &&
                membership.UserId == actorId &&
                membership.Role == MembershipRole.Owner));
        if (afterCreatedAt is not null)
        {
            query = query.Where(workspace =>
                workspace.CreatedAt > afterCreatedAt.Value ||
                workspace.CreatedAt == afterCreatedAt.Value && workspace.Id.CompareTo(afterId!.Value) > 0);
        }

        return await query
            .OrderBy(workspace => workspace.CreatedAt)
            .ThenBy(workspace => workspace.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
