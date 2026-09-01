using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Knowledge.Server.Infrastructure.Persistence;

public sealed class SqliteKnowledgeDbContextFactory
    : IDesignTimeDbContextFactory<SqliteKnowledgeDbContext>
{
    public SqliteKnowledgeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<SqliteKnowledgeDbContext>()
            .UseSqlite("Data Source=knowledge-design.db")
            .Options;
        return new SqliteKnowledgeDbContext(options);
    }
}

public sealed class PostgreSqlKnowledgeDbContextFactory
    : IDesignTimeDbContextFactory<PostgreSqlKnowledgeDbContext>
{
    public PostgreSqlKnowledgeDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PostgreSqlKnowledgeDbContext>()
            .UseNpgsql("Host=localhost;Database=knowledge_design;Username=knowledge;Password=design-only")
            .Options;
        return new PostgreSqlKnowledgeDbContext(options);
    }
}
