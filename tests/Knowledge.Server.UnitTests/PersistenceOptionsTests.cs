using System.ComponentModel.DataAnnotations;
using Knowledge.Server.Infrastructure.Persistence;

namespace Knowledge.Server.UnitTests;

public sealed class PersistenceOptionsTests
{
    [Theory]
    [InlineData("Sqlite", PersistenceProvider.Sqlite)]
    [InlineData("sqlite", PersistenceProvider.Sqlite)]
    [InlineData("PostgreSql", PersistenceProvider.PostgreSql)]
    public void ParseProvider_AcceptsSupportedValues(string value, PersistenceProvider expected)
    {
        var options = new PersistenceOptions { Provider = value };

        Assert.Equal(expected, options.ParseProvider());
    }

    [Fact]
    public void ParseProvider_RejectsUnsupportedValue()
    {
        var options = new PersistenceOptions { Provider = "Unknown" };

        var exception = Assert.Throws<InvalidOperationException>(() => options.ParseProvider());

        Assert.Contains("Supported values are Sqlite and PostgreSql", exception.Message);
    }

    [Fact]
    public void Validate_RequiresPostgreSqlConnectionString()
    {
        var options = new PersistenceOptions
        {
            Provider = "PostgreSql",
            PostgreSqlConnectionString = null,
        };

        var results = options.Validate(new ValidationContext(options)).ToArray();

        Assert.Single(results);
        Assert.Contains("connection string is required", results[0].ErrorMessage);
        Assert.Equal([nameof(PersistenceOptions.PostgreSqlConnectionString)], results[0].MemberNames);
    }
}
