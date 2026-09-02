namespace Knowledge.Server.Knowledge.Features;

public enum ArticleResultStatus
{
    Created,
    Found,
    Updated,
    ValidationFailed,
    NotFound,
    RevisionConflict,
}

public sealed record ArticleResult(
    ArticleResultStatus Status,
    Article? Article = null,
    IReadOnlyDictionary<string, string[]>? Errors = null,
    int? CurrentRevisionVersion = null);
