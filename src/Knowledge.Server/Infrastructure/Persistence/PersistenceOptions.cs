using System.ComponentModel.DataAnnotations;

namespace Knowledge.Server.Infrastructure.Persistence;

public sealed class PersistenceOptions : IValidatableObject
{
    public const string SectionName = "Persistence";

    [Required]
    public string Provider { get; init; } = nameof(PersistenceProvider.Sqlite);

    public string SqliteConnectionString { get; init; } = "Data Source=data/knowledge.db";

    public string? PostgreSqlConnectionString { get; init; }

    public PersistenceProvider ParseProvider()
    {
        if (Enum.TryParse<PersistenceProvider>(Provider, ignoreCase: true, out var provider))
        {
            return provider;
        }

        throw new InvalidOperationException(
            $"Unsupported persistence provider '{Provider}'. Supported values are Sqlite and PostgreSql.");
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        PersistenceProvider provider;
        try
        {
            provider = ParseProvider();
        }
        catch (InvalidOperationException exception)
        {
            return [new ValidationResult(exception.Message, [nameof(Provider)])];
        }

        var selectedConnectionString = provider == PersistenceProvider.Sqlite
            ? SqliteConnectionString
            : PostgreSqlConnectionString;
        var connectionStringMember = provider == PersistenceProvider.Sqlite
            ? nameof(SqliteConnectionString)
            : nameof(PostgreSqlConnectionString);

        if (string.IsNullOrWhiteSpace(selectedConnectionString))
        {
            return [new ValidationResult(
                $"A connection string is required for persistence provider {provider}.",
                [connectionStringMember])];
        }

        return [];
    }
}
