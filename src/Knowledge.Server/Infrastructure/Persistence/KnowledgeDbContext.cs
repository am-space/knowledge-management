using Microsoft.EntityFrameworkCore;

namespace Knowledge.Server.Infrastructure.Persistence;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options);
