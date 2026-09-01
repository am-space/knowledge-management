using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.Infrastructure.Persistence;

public sealed class SqliteKnowledgeDbContext(DbContextOptions<SqliteKnowledgeDbContext> options)
    : KnowledgeDbContext(options);
