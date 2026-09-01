namespace Knowledge.Server.Knowledge.Domain;

public sealed class KnowledgeNode
{
    private readonly List<KnowledgeRevision> _revisions = [];

    private KnowledgeNode()
    {
    }

    public static KnowledgeNode CreateArticle(
        Guid id,
        Guid workspaceId,
        Guid initialRevisionId,
        string title,
        string contentMarkdown,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        ValidateIdentity(id, workspaceId, createdBy);

        var node = new KnowledgeNode
        {
            Id = id,
            WorkspaceId = workspaceId,
            Type = KnowledgeNodeType.Article,
            Status = KnowledgeNodeStatus.Active,
            CreatedBy = createdBy,
            CreatedAt = createdAt,
        };
        var revision = new KnowledgeRevision(
            initialRevisionId,
            workspaceId,
            id,
            1,
            title,
            contentMarkdown,
            createdBy,
            createdAt);

        node._revisions.Add(revision);
        node.CurrentRevision = revision;
        node.CurrentRevisionId = revision.Id;
        return node;
    }

    public KnowledgeRevision AddRevision(
        Guid revisionId,
        int expectedVersion,
        string title,
        string contentMarkdown,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        if (CurrentRevision is null)
        {
            throw new InvalidOperationException("The node has no current revision.");
        }

        if (expectedVersion != CurrentRevision.Version)
        {
            throw new RevisionConflictException(expectedVersion, CurrentRevision.Version);
        }

        var revision = new KnowledgeRevision(
            revisionId,
            WorkspaceId,
            Id,
            checked(CurrentRevision.Version + 1),
            title,
            contentMarkdown,
            createdBy,
            createdAt);

        _revisions.Add(revision);
        CurrentRevision = revision;
        CurrentRevisionId = revision.Id;
        return revision;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid? ParentId { get; private set; }

    public KnowledgeNodeType Type { get; private set; }

    public Guid? CurrentRevisionId { get; private set; }

    public KnowledgeRevision? CurrentRevision { get; private set; }

    public KnowledgeNodeStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }

    public IReadOnlyCollection<KnowledgeRevision> Revisions => _revisions.AsReadOnly();

    private static void ValidateIdentity(Guid id, Guid workspaceId, Guid createdBy)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A node ID is required.", nameof(id));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("A workspace ID is required.", nameof(workspaceId));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user ID is required.", nameof(createdBy));
        }
    }
}
