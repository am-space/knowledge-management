using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.Infrastructure.Persistence;

public sealed class PostgreSqlKnowledgeDbContext(
    DbContextOptions<PostgreSqlKnowledgeDbContext> options)
    : KnowledgeDbContext(options);
