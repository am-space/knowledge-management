using Knowledge.Server.Workspaces.Features;

namespace Knowledge.Server.Workspaces.Infrastructure;

public sealed class UnavailableWorkspaceContext : IWorkspaceContext
{
    public Guid WorkspaceId => throw new WorkspaceAccessDeniedException();

    public Guid ActorId => throw new WorkspaceAccessDeniedException();
}
