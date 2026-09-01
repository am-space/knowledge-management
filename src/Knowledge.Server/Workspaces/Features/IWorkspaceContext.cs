namespace Knowledge.Server.Workspaces.Features;

public interface IWorkspaceContext
{
    Guid WorkspaceId { get; }

    Guid ActorId { get; }
}
