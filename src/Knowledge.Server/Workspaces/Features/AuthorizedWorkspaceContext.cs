namespace Knowledge.Server.Workspaces.Features;

public sealed class AuthorizedWorkspaceContext : IWorkspaceContext
{
    internal AuthorizedWorkspaceContext(Guid workspaceId, Guid actorId)
    {
        WorkspaceId = workspaceId;
        ActorId = actorId;
    }

    public Guid WorkspaceId { get; }

    public Guid ActorId { get; }
}
