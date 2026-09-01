using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using OnlineTeacher.Infrastructure.Tenancy;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling. Reads the connection string from the
/// ConnectionStrings__DefaultConnection environment variable, or builds one from POSTGRES_*
/// environment variables. Placeholder defaults mirror the local docker-compose dev stack only.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(GetConnectionString())
            .Options;

        return new ApplicationDbContext(options, new TenantContext());
    }

    private static string GetConnectionString()
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