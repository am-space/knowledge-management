namespace Knowledge.Server.Knowledge.Presentation;

internal static class ArticleProblems
{
    private const string ValidationType = "urn:knowledge:problem:validation";

    public static IResult Validation(
        HttpContext context,
        IReadOnlyDictionary<string, string[]> errors) =>
        Results.Problem(
            type: ValidationType,
            title: "One or more validation errors occurred.",
            statusCode: StatusCodes.Status400BadRequest,
            extensions: Extensions(context, ("errors", errors)));

    public static IResult NotFound(HttpContext context) => Results.Problem(
        type: "urn:knowledge:problem:article-not-found",
        title: "Article not found.",
        statusCode: StatusCodes.Status404NotFound,
        extensions: Extensions(context));

    public static IResult RevisionConflict(HttpContext context, int currentRevisionVersion) =>
        Results.Problem(
            type: "urn:knowledge:problem:revision-conflict",
            title: "The Article has been updated since it was read.",
            statusCode: StatusCodes.Status409Conflict,
            extensions: Extensions(
                context,
                ("currentRevisionVersion", currentRevisionVersion)));

    public static IResult WorkspaceAccessDenied(HttpContext context) => Results.Problem(
        type: "urn:knowledge:problem:workspace-access-denied",
        title: "The active workspace could not be resolved.",
        statusCode: StatusCodes.Status403Forbidden,
        extensions: Extensions(context));

    public static IResult Unexpected(HttpContext context) => Results.Problem(
        title: "An unexpected error occurred.",
        statusCode: StatusCodes.Status500InternalServerError,
        extensions: Extensions(context));

    private static Dictionary<string, object?> Extensions(
        HttpContext context,
        params (string Name, object? Value)[] values)
    {
        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["traceId"] = context.TraceIdentifier,
        };

        foreach (var (name, value) in values)
        {
            extensions[name] = value;
        }

        return extensions;
    }
}
