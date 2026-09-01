namespace Knowledge.Server.Workspaces.Domain;

public sealed class User
{
    public const int MaxDisplayNameLength = 200;

    private User()
    {
    }

    public User(Guid id, string displayName, DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.", nameof(id));
        }

        var normalizedDisplayName = displayName?.Trim();
        if (string.IsNullOrEmpty(normalizedDisplayName))
        {
            throw new ArgumentException("A display name is required.", nameof(displayName));
        }

        if (normalizedDisplayName.Length > MaxDisplayNameLength)
        {
            throw new ArgumentException(
                $"A display name cannot exceed {MaxDisplayNameLength} characters.",
                nameof(displayName));
        }

        Id = id;
        DisplayName = normalizedDisplayName;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public string DisplayName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }
}
