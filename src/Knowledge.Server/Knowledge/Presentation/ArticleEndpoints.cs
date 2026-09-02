using Knowledge.Server.Knowledge.Features;
using Knowledge.Server.Workspaces.Features;

namespace Knowledge.Server.Knowledge.Presentation;

public static class ArticleEndpoints
{
    public static IEndpointRouteBuilder MapArticleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var articles = endpoints.MapGroup("/api/articles");

        articles.MapPost("/", CreateAsync);
        articles.MapGet("/{id}", GetAsync);
        articles.MapPut("/{id}", UpdateAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateArticleRequest request,
        ArticleService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(
            request.Title,
            request.ContentMarkdown,
            cancellationToken);

        return result.Status switch
        {
            ArticleResultStatus.Created => Results.Created(
                $"/api/articles/{result.Article!.Id:D}",
                ArticleResponse.From(result.Article)),
            ArticleResultStatus.ValidationFailed => ArticleProblems.Validation(
                httpContext,
                result.Errors!),
            _ => throw UnexpectedStatus(result.Status),
        };
    }

    private static async Task<IResult> GetAsync(
        string id,
        ArticleService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseCanonicalId(id, out var articleId))
        {
            return ArticleProblems.Validation(
                httpContext,
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["id"] = ["Article ID must be a canonical lowercase hyphenated UUID."],
                });
        }

        var result = await service.GetAsync(articleId, cancellationToken);
        return result.Status switch
        {
            ArticleResultStatus.Found => Results.Ok(ArticleResponse.From(result.Article!)),
            ArticleResultStatus.NotFound => ArticleProblems.NotFound(httpContext),
            _ => throw UnexpectedStatus(result.Status),
        };
    }

    private static async Task<IResult> UpdateAsync(
        string id,
        UpdateArticleRequest request,
        ArticleService service,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!TryParseCanonicalId(id, out var articleId))
        {
            return ArticleProblems.Validation(
                httpContext,
                new Dictionary<string, string[]>(StringComparer.Ordinal)
                {
                    ["id"] = ["Article ID must be a canonical lowercase hyphenated UUID."],
                });
        }

        var result = await service.UpdateAsync(
            articleId,
            request.ExpectedRevisionVersion,
            request.Title,
            request.ContentMarkdown,
            cancellationToken);

        return result.Status switch
        {
            ArticleResultStatus.Updated => Results.Ok(ArticleResponse.From(result.Article!)),
            ArticleResultStatus.ValidationFailed => ArticleProblems.Validation(
                httpContext,
                result.Errors!),
            ArticleResultStatus.NotFound => ArticleProblems.NotFound(httpContext),
            ArticleResultStatus.RevisionConflict => ArticleProblems.RevisionConflict(
                httpContext,
                result.CurrentRevisionVersion!.Value),
            _ => throw UnexpectedStatus(result.Status),
        };
    }

    private static InvalidOperationException UnexpectedStatus(ArticleResultStatus status) =>
        new($"ArticleService returned unexpected status '{status}'.");

    private static bool TryParseCanonicalId(string value, out Guid id) =>
        Guid.TryParseExact(value, "D", out id) &&
        string.Equals(value, id.ToString("D"), StringComparison.Ordinal);

}

public sealed record CreateArticleRequest(string? Title, string? ContentMarkdown);

public sealed record UpdateArticleRequest(
    int ExpectedRevisionVersion,
    string? Title,
    string? ContentMarkdown);

public sealed record ArticleResponse(
    Guid Id,
    string Type,
    DateTime CreatedAt,
    Guid CreatedBy,
    ArticleRevisionResponse CurrentRevision)
{
    internal static ArticleResponse From(Article article) => new(
        article.Id,
        "article",
        article.CreatedAt.UtcDateTime,
        article.CreatedBy,
        ArticleRevisionResponse.From(article.CurrentRevision));
}

public sealed record ArticleRevisionResponse(
    Guid Id,
    int Version,
    string Title,
    string ContentMarkdown,
    DateTime CreatedAt,
    Guid CreatedBy)
{
    internal static ArticleRevisionResponse From(ArticleRevision revision) => new(
        revision.Id,
        revision.Version,
        revision.Title,
        revision.ContentMarkdown,
        revision.CreatedAt.UtcDateTime,
        revision.CreatedBy);
}
