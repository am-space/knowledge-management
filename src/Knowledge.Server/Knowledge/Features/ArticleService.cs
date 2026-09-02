using Knowledge.Server.Infrastructure.Persistence;
using Knowledge.Server.Knowledge.Domain;
using Knowledge.Server.Workspaces.Features;
using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.Knowledge.Features;

public sealed class ArticleService(
    KnowledgeDbContext dbContext,
    IWorkspaceContext workspaceContext,
    TimeProvider timeProvider)
{
    public async Task<ArticleResult> CreateAsync(
        string? title,
        string? contentMarkdown,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateContent(title, contentMarkdown);
        if (validation is not null)
        {
            return validation;
        }

        var now = timeProvider.GetUtcNow();
        var node = KnowledgeNode.CreateArticle(
            Guid.NewGuid(),
            workspaceContext.WorkspaceId,
            Guid.NewGuid(),
            title!,
            contentMarkdown!,
            workspaceContext.ActorId,
            now);

        dbContext.KnowledgeNodes.Add(node);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ArticleResult(ArticleResultStatus.Created, Map(node, node.CurrentRevision!));
    }

    public async Task<ArticleResult> GetAsync(
        Guid nodeId,
        CancellationToken cancellationToken = default)
    {
        var article = await FindCurrentArticleAsync(nodeId, cancellationToken);
        return article is null
            ? new ArticleResult(ArticleResultStatus.NotFound)
            : new ArticleResult(ArticleResultStatus.Found, article);
    }

    public async Task<ArticleResult> UpdateAsync(
        Guid nodeId,
        int expectedRevisionVersion,
        string? title,
        string? contentMarkdown,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidateUpdate(expectedRevisionVersion, title, contentMarkdown);
        if (validation is not null)
        {
            return validation;
        }

        var current = await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node =>
                node.WorkspaceId == workspaceContext.WorkspaceId &&
                node.Id == nodeId &&
                node.Type == KnowledgeNodeType.Article)
            .Select(node => new CurrentArticle(
                node.Id,
                node.CreatedAt,
                node.CreatedBy,
                node.CurrentRevisionId,
                node.CurrentRevision == null ? 0 : node.CurrentRevision.Version))
            .SingleOrDefaultAsync(cancellationToken);

        if (current is null || current.CurrentRevisionId is null)
        {
            return new ArticleResult(ArticleResultStatus.NotFound);
        }

        if (current.Version != expectedRevisionVersion)
        {
            return Conflict(current.Version);
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var revision = new KnowledgeRevision(
            Guid.NewGuid(),
            workspaceContext.WorkspaceId,
            nodeId,
            checked(expectedRevisionVersion + 1),
            title!,
            contentMarkdown!,
            workspaceContext.ActorId,
            timeProvider.GetUtcNow());

        dbContext.KnowledgeRevisions.Add(revision);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);

            var updated = await dbContext.KnowledgeNodes
                .Where(node =>
                    node.WorkspaceId == workspaceContext.WorkspaceId &&
                    node.Id == nodeId &&
                    node.Type == KnowledgeNodeType.Article &&
                    node.CurrentRevisionId == current.CurrentRevisionId)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        node => node.CurrentRevisionId,
                        revision.Id),
                    cancellationToken);

            if (updated == 0)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                dbContext.Entry(revision).State = EntityState.Detached;
                return await ResolveRejectedUpdateAsync(
                        nodeId,
                        expectedRevisionVersion,
                        cancellationToken)
                    ?? throw new InvalidOperationException(
                        "The Article pointer update was rejected without a revision conflict.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.Entry(revision).State = EntityState.Detached;

            var rejected = await ResolveRejectedUpdateAsync(
                nodeId,
                expectedRevisionVersion,
                cancellationToken);
            if (rejected is not null)
            {
                return rejected;
            }

            throw;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            dbContext.Entry(revision).State = EntityState.Detached;
            throw;
        }

        return new ArticleResult(
            ArticleResultStatus.Updated,
            new Article(
                current.Id,
                KnowledgeNodeType.Article,
                current.CreatedAt,
                current.CreatedBy,
                Map(revision)));
    }

    private async Task<Article?> FindCurrentArticleAsync(Guid nodeId, CancellationToken cancellationToken) =>
        await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node =>
                node.WorkspaceId == workspaceContext.WorkspaceId &&
                node.Id == nodeId &&
                node.Type == KnowledgeNodeType.Article &&
                node.CurrentRevision != null)
            .Select(node => new Article(
                node.Id,
                node.Type,
                node.CreatedAt,
                node.CreatedBy,
                new ArticleRevision(
                    node.CurrentRevision!.Id,
                    node.CurrentRevision.Version,
                    node.CurrentRevision.Title,
                    node.CurrentRevision.ContentMarkdown,
                    node.CurrentRevision.CreatedAt,
                    node.CurrentRevision.CreatedBy)))
            .SingleOrDefaultAsync(cancellationToken);

    private async Task<ArticleResult?> ResolveRejectedUpdateAsync(
        Guid nodeId,
        int expectedRevisionVersion,
        CancellationToken cancellationToken)
    {
        var version = await dbContext.KnowledgeNodes
            .AsNoTracking()
            .Where(node =>
                node.WorkspaceId == workspaceContext.WorkspaceId &&
                node.Id == nodeId &&
                node.Type == KnowledgeNodeType.Article)
            .Select(node => node.CurrentRevision == null ? (int?)null : node.CurrentRevision.Version)
            .SingleOrDefaultAsync(cancellationToken);

        if (version is null)
        {
            return new ArticleResult(ArticleResultStatus.NotFound);
        }

        return version.Value != expectedRevisionVersion
            ? Conflict(version.Value)
            : null;
    }

    private static ArticleResult? ValidateUpdate(
        int expectedRevisionVersion,
        string? title,
        string? contentMarkdown)
    {
        var errors = GetContentErrors(title, contentMarkdown);
        if (expectedRevisionVersion < 1)
        {
            errors["expectedRevisionVersion"] = ["Expected revision version must be positive."];
        }

        return errors.Count == 0
            ? null
            : new ArticleResult(ArticleResultStatus.ValidationFailed, Errors: errors);
    }

    private static ArticleResult? ValidateContent(string? title, string? contentMarkdown)
    {
        var errors = GetContentErrors(title, contentMarkdown);
        return errors.Count == 0
            ? null
            : new ArticleResult(ArticleResultStatus.ValidationFailed, Errors: errors);
    }

    private static Dictionary<string, string[]> GetContentErrors(
        string? title,
        string? contentMarkdown)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var normalizedTitle = title?.Trim();
        if (string.IsNullOrEmpty(normalizedTitle))
        {
            errors["title"] = ["Title is required."];
        }
        else if (normalizedTitle.Length > KnowledgeRevision.MaxTitleLength)
        {
            errors["title"] = [$"Title cannot exceed {KnowledgeRevision.MaxTitleLength} characters."];
        }

        if (contentMarkdown is null)
        {
            errors["contentMarkdown"] = ["Content Markdown is required."];
        }

        return errors;
    }

    private static Article Map(KnowledgeNode node, KnowledgeRevision revision) => new(
        node.Id,
        node.Type,
        node.CreatedAt,
        node.CreatedBy,
        Map(revision));

    private static ArticleRevision Map(KnowledgeRevision revision) => new(
        revision.Id,
        revision.Version,
        revision.Title,
        revision.ContentMarkdown,
        revision.CreatedAt,
        revision.CreatedBy);

    private static ArticleResult Conflict(int currentVersion) => new(
        ArticleResultStatus.RevisionConflict,
        CurrentRevisionVersion: currentVersion);

    private sealed record CurrentArticle(
        Guid Id,
        DateTimeOffset CreatedAt,
        Guid CreatedBy,
        Guid? CurrentRevisionId,
        int Version);
}
