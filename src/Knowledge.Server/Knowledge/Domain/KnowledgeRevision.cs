namespace Knowledge.Server.Knowledge.Domain;

public sealed class KnowledgeRevision
{
    public const int MaxTitleLength = 500;

    private KnowledgeRevision()
    {
    }

    internal KnowledgeRevision(
        Guid id,
        Guid workspaceId,
        Guid nodeId,
        int version,
        string title,
        string contentMarkdown,
        Guid createdBy,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A revision ID is required.", nameof(id));
        }

        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("A workspace ID is required.", nameof(workspaceId));
        }

        if (nodeId == Guid.Empty)
        {
            throw new ArgumentException("A node ID is required.", nameof(nodeId));
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "A revision version must be positive.");
        }

        var normalizedTitle = title?.Trim();
        if (string.IsNullOrEmpty(normalizedTitle))
        {
            throw new ArgumentException("A revision title is required.", nameof(title));
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException(
                $"A revision title cannot exceed {MaxTitleLength} characters.",
                nameof(title));
        }

        ArgumentNullException.ThrowIfNull(contentMarkdown);

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user ID is required.", nameof(createdBy));
        }

        Id = id;
        WorkspaceId = workspaceId;
        NodeId = nodeId;
        Version = version;
        Title = normalizedTitle;
        ContentMarkdown = contentMarkdown;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid WorkspaceId { get; private set; }

    public Guid NodeId { get; private set; }

    public int Version { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string ContentMarkdown { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }
}
