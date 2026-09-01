using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Knowledge.Server.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<SqliteKnowledgeDbContext>((serviceProvider, dbOptions) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            dbOptions.UseSqlite(
                ResolveSqlitePath(options.SqliteConnectionString, contentRootPath));
        });
        services.AddDbContext<PostgreSqlKnowledgeDbContext>((serviceProvider, dbOptions) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            dbOptions.UseNpgsql(options.PostgreSqlConnectionString);
        });
        services.AddScoped<KnowledgeDbContext>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return options.ParseProvider() == PersistenceProvider.Sqlite
                ? serviceProvider.GetRequiredService<SqliteKnowledgeDbContext>()
                : serviceProvider.GetRequiredService<PostgreSqlKnowledgeDbContext>();
        });

        return services;
    }

    private static string ResolveSqlitePath(string connectionString, string contentRootPath)
    {
        var builder = new SqliteConnectionStringBuilder(connectionString)
        {
            ForeignKeys = true,
            Pooling = true,
            DefaultTimeout = 30,
        };

        if (!string.IsNullOrWhiteSpace(builder.DataSource) && !Path.IsPathRooted(builder.DataSource))
        {
            builder.DataSource = Path.GetFullPath(builder.DataSource, contentRootPath);
        }

        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return builder.ConnectionString;
    }
}
