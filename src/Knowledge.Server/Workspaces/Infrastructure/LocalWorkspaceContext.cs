using Knowledge.Server.Workspaces.Features;

namespace Knowledge.Server.Workspaces.Infrastructure;

public sealed class LocalWorkspaceContext : IWorkspaceContext
{
    public static readonly Guid OwnerId = new("01996e76-6d91-74fb-8dd4-f8ce217b6bd5");

    public static readonly Guid PersonalWorkspaceId = new("01996e76-6d91-74fb-8dd4-f8ce217b6bd6");

    public Guid WorkspaceId => PersonalWorkspaceId;

    public Guid ActorId => OwnerId;
}
