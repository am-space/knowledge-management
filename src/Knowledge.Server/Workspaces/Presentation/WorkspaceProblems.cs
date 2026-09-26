namespace Knowledge.Server.Workspaces.Presentation;

internal static class WorkspaceProblems
{
    public static IResult AccessDenied(HttpContext context) => Results.Problem(
        type: "urn:knowledge:problem:workspace-access-denied",
        title: "A trusted actor is required.",
        statusCode: StatusCodes.Status403Forbidden,
        extensions: Extensions(context));

    public static IResult NotFound(HttpContext context) => Results.Problem(
        type: "urn:knowledge:problem:workspace-not-found",
        title: "Workspace not found.",
        statusCode: StatusCodes.Status404NotFound,
        extensions: Extensions(context));

    private static Dictionary<string, object?> Extensions(HttpContext context) => new(StringComparer.Ordinal)
    {
        ["traceId"] = context.TraceIdentifier,
    };
}
