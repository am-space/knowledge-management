namespace Knowledge.Server.Workspaces.Features;

public sealed class WorkspaceAccessDeniedException : Exception
{
    public WorkspaceAccessDeniedException()
        : base("The active workspace could not be resolved.")
    {
    }
}
