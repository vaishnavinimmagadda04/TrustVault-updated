using Npgsql;

namespace TrustVault.API.Data;

public static class DatabaseConnectionSettings
{
    public static string ResolveProvider(string? configuredProvider, string connectionString)
    {
        if (IsPostgresUrl(connectionString))
        {
            return "PostgreSQL";
        }

        if (!string.IsNullOrWhiteSpace(configuredProvider))
        {
            return configuredProvider;
        }

        return "SqlServer";
    }

    public static string NormalizePostgresConnectionString(string connectionString)
    {
        if (!Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) || !IsPostgresScheme(uri.Scheme))
        {
            return connectionString;
        }

        var credentials = uri.UserInfo.Split(':', 2);
        var database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));
        if (credentials.Length != 2 || string.IsNullOrWhiteSpace(database))
        {
            throw new InvalidOperationException("The PostgreSQL URL must contain a database name, username, and password.");
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = database,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = Uri.UnescapeDataString(credentials[1]),
            SslMode = SslMode.Require
        };

        foreach (var parameter in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = parameter.Split('=', 2);
            if (parts.Length == 2 && parts[0].Equals("sslmode", StringComparison.OrdinalIgnoreCase))
            {
                builder.SslMode = Enum.Parse<SslMode>(Uri.UnescapeDataString(parts[1]), true);
            }
        }

        return builder.ConnectionString;
    }

    private static bool IsPostgresUrl(string connectionString) =>
        Uri.TryCreate(connectionString, UriKind.Absolute, out var uri) && IsPostgresScheme(uri.Scheme);

    private static bool IsPostgresScheme(string scheme) =>
        scheme.Equals("postgres", StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals("postgresql", StringComparison.OrdinalIgnoreCase);
}
