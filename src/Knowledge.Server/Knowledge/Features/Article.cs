using Knowledge.Server.Knowledge.Domain;

namespace Knowledge.Server.Knowledge.Features;

public sealed record Article(
    Guid Id,
    KnowledgeNodeType Type,
    DateTimeOffset CreatedAt,
    Guid CreatedBy,
    ArticleRevision CurrentRevision);

public sealed record ArticleRevision(
    Guid Id,
    int Version,
    string Title,
    string ContentMarkdown,
    DateTimeOffset CreatedAt,
    Guid CreatedBy);
