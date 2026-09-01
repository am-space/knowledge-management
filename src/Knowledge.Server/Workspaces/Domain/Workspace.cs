namespace Knowledge.Server.Workspaces.Domain;

public sealed class Workspace
{
    private Workspace()
    {
    }

    public Workspace(Guid id, string name, Guid createdBy, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A workspace ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("A workspace name is required.", nameof(name));
        }

        if (createdBy == Guid.Empty)
        {
            throw new ArgumentException("A creating user ID is required.", nameof(createdBy));
        }

        Id = id;
        Name = name.Trim();
        CreatedBy = createdBy;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public Guid CreatedBy { get; private set; }
}
