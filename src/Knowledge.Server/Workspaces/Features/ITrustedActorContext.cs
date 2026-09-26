namespace Knowledge.Server.Workspaces.Features;

// Supplied by the host, never by an HTTP request. Workspace operations do not need an active workspace.
public interface ITrustedActorContext
{
    Guid ActorId { get; }

    Guid DefaultWorkspaceId { get; }
}
