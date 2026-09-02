using Npgsql;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Builds the PostgreSQL connection string from environment configuration.
/// Prefers ConnectionStrings__DefaultConnection; otherwise builds one from
/// POSTGRES_* variables. Placeholder defaults mirror the local docker-compose
/// development stack only and must never be treated as real credentials.
/// </summary>
public static class ConnectionFactory
{
    public static string Build()
    {
        var fromConfiguration = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fromConfiguration))
        {
            return fromConfiguration;
        }

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("POSTGRES_HOST") ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable("POSTGRES_PORT") ?? "5432"),
            Database = Environment.GetEnvironmentVariable("POSTGRES_DB") ?? "onlineteacher",
            Username = Environment.GetEnvironmentVariable("POSTGRES_USER") ?? "onlineteacher",
            Password = Environment.GetEnvironmentVariable("POSTGRES_PASSWORD") ?? "onlineteacher_dev"
        };

        return builder.ConnectionString;
    }
}