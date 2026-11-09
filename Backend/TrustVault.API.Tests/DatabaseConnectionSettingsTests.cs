using TrustVault.API.Data;

namespace TrustVault.API.Tests;

public sealed class DatabaseConnectionSettingsTests
{
    [Theory]
    [InlineData("postgres://user:password@localhost:5432/trustvault")]
    [InlineData("postgresql://user:password@localhost:5432/trustvault")]
    public void PostgresUrlSelectsPostgresEvenWhenSqlServerIsConfigured(string connectionString)
    {
        var provider = DatabaseConnectionSettings.ResolveProvider("SqlServer", connectionString);

        Assert.Equal("PostgreSQL", provider);
    }

    [Fact]
    public void SqlServerConnectionStringUsesConfiguredProvider()
    {
        const string connectionString = "Server=localhost;Database=TrustVault;";

        var provider = DatabaseConnectionSettings.ResolveProvider("SqlServer", connectionString);

        Assert.Equal("SqlServer", provider);
    }
}
