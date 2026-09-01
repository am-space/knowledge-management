namespace Knowledge.Server.Workspaces.Domain;

public sealed class Workspace
{
    public const int MaxNameLength = 200;

    private Workspace()
    {
    }

    public Workspace(Guid id, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A workspace ID is required.", nameof(id));
        }

        var normalizedName = name?.Trim();
        if (string.IsNullOrEmpty(normalizedName))
        {
            throw new ArgumentException("A workspace name is required.", nameof(name));
        }

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"A workspace name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user ID is required.", nameof(createdBy));
        }

        Id = id;
        Name = normalizedName;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }
}
