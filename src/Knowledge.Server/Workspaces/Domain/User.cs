namespace Knowledge.Server.Workspaces.Domain;

public sealed class User
{
    private User()
    {
    }

    public User(Guid id, string displayName, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        Id = id;
        DisplayName = displayName.Trim();
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
}
