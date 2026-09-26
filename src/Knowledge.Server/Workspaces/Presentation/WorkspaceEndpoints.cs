using System.Globalization;
using Knowledge.Server.Knowledge.Presentation;
using Knowledge.Server.Workspaces.Features;
using Microsoft.Extensions.Primitives;

namespace Knowledge.Server.Workspaces.Presentation;

public static class WorkspaceEndpoints
{
    public static IEndpointRouteBuilder MapWorkspaceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var workspaces = endpoints.MapGroup("/api/workspaces");
        workspaces.MapGet("/", ListAsync);
        workspaces.MapPost("/", CreateAsync);
        workspaces.MapGet("/{workspaceId}", GetAsync);
        workspaces.MapPut("/{workspaceId}", RenameAsync);
        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        WorkspaceService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        var pageSize = WorkspaceService.DefaultPageSize;
        if (context.Request.Query.TryGetValue("pageSize", out var requestedPageSize) &&
            (requestedPageSize.Count != 1 ||
             !int.TryParse(requestedPageSize[0], NumberStyles.None, CultureInfo.InvariantCulture, out pageSize)))
        {
            errors["pageSize"] = ["Page size must be an integer between 1 and 100."];
        }

        string? cursor = null;
        if (context.Request.Query.TryGetValue("cursor", out StringValues requestedCursor))
        {
            if (requestedCursor.Count != 1)
            {
                errors["cursor"] = ["Cursor must be supplied once."];
            }
            else
            {
                cursor = requestedCursor[0];
            }
        }

        if (errors.Count != 0)
        {
            return ArticleProblems.Validation(context, errors);
        }

        var result = await service.ListAsync(pageSize, cursor, cancellationToken);
        return result.Errors is not null
            ? ArticleProblems.Validation(context, result.Errors)
            : Results.Ok(WorkspacePageResponse.From(result.Page!));
    }

    private static async Task<IResult> CreateAsync(
        WorkspaceNameRequest request,
        WorkspaceService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request.Name, cancellationToken);
        return result.Status switch
        {
            WorkspaceResultStatus.Created => Results.Created(
                $"/api/workspaces/{result.Workspace!.Id:D}",
                WorkspaceResponse.From(result.Workspace)),
            WorkspaceResultStatus.ValidationFailed => ArticleProblems.Validation(context, result.Errors!),
            _ => throw UnexpectedStatus(result.Status),
        };
    }

    private static async Task<IResult> GetAsync(
        string workspaceId,
        WorkspaceService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!TryParseCanonicalId(workspaceId, out var id))
        {
            return InvalidId(context);
        }

        var workspace = await service.GetAsync(id, cancellationToken);
        return workspace is null
            ? WorkspaceProblems.NotFound(context)
            : Results.Ok(WorkspaceResponse.From(workspace));
    }

    private static async Task<IResult> RenameAsync(
        string workspaceId,
        WorkspaceNameRequest request,
        WorkspaceService service,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        if (!TryParseCanonicalId(workspaceId, out var id))
        {
            return InvalidId(context);
        }

        var result = await service.RenameAsync(id, request.Name, cancellationToken);
        return result.Status switch
        {
            WorkspaceResultStatus.Updated => Results.Ok(WorkspaceResponse.From(result.Workspace!)),
            WorkspaceResultStatus.ValidationFailed => ArticleProblems.Validation(context, result.Errors!),
            WorkspaceResultStatus.NotFound => WorkspaceProblems.NotFound(context),
            _ => throw UnexpectedStatus(result.Status),
        };
    }

    private static IResult InvalidId(HttpContext context) => ArticleProblems.Validation(
        context,
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["workspaceId"] = ["Workspace ID must be a canonical lowercase hyphenated UUID."],
        });

    private static bool TryParseCanonicalId(string value, out Guid id) =>
        Guid.TryParseExact(value, "D", out id) &&
        string.Equals(value, id.ToString("D"), StringComparison.Ordinal);

    private static InvalidOperationException UnexpectedStatus(WorkspaceResultStatus status) =>
        new($"WorkspaceService returned unexpected status '{status}'.");
}

public sealed record WorkspaceNameRequest(string? Name);

public sealed record WorkspaceResponse(Guid Id, string Name, DateTime CreatedAt, Guid CreatedBy)
{
    internal static WorkspaceResponse From(WorkspaceInfo workspace) => new(
        workspace.Id,
        workspace.Name,
        workspace.CreatedAt.UtcDateTime,
        workspace.CreatedBy);
}

public sealed record WorkspacePageResponse(
    IReadOnlyList<WorkspaceResponse> Items,
    string? NextCursor,
    Guid DefaultWorkspaceId)
{
    internal static WorkspacePageResponse From(WorkspacePage page) => new(
        page.Items.Select(WorkspaceResponse.From).ToList(),
        page.NextCursor,
        page.DefaultWorkspaceId);
}
