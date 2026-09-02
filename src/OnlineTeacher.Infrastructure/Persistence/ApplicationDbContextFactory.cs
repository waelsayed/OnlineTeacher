using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OnlineTeacher.Infrastructure.Tenancy;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core tooling. Uses <see cref="ConnectionFactory"/> so
/// EF tools and the API runtime build the connection string the same way.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionFactory.Build())
            .Options;

        return new ApplicationDbContext(options, new TenantContext());
    }
}