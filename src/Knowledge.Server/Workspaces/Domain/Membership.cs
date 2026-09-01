namespace Knowledge.Server.Workspaces.Domain;

public sealed class Membership
{
    private Membership()
    {
    }

    public Membership(
        Guid workspaceId,
        Guid userId,
        MembershipRole role,
        DateTimeOffset joinedAt)
    {
        if (workspaceId == Guid.Empty)
        {
            throw new ArgumentException("A workspace ID is required.", nameof(workspaceId));
        }

        if (userId == Guid.Empty)
        {
            throw new ArgumentException("A user ID is required.", nameof(userId));
        }

        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        WorkspaceId = workspaceId;
        UserId = userId;
        Role = role;
        JoinedAt = joinedAt;
    }

    public Guid WorkspaceId { get; private set; }

    public Guid UserId { get; private set; }

    public MembershipRole Role { get; private set; }

    public DateTimeOffset JoinedAt { get; private set; }
}
