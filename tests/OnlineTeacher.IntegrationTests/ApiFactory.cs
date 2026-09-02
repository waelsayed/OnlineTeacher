using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// Boots a disposable PostgreSQL Testcontainer once per test collection and hosts the API
/// via WebApplicationFactory pointed at it. Startup migration and permission seeding run
/// through the real Program entry point.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("onlineteacher_test")
        .WithUsername("onlineteacher")
        .WithPassword("onlineteacher_test_pwd")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString(),
                ["Jwt:Issuer"] = "OnlineTeacher.IntegrationTests",
                ["Jwt:Audience"] = "OnlineTeacher.IntegrationTests",
                ["Jwt:SigningKey"] = "integration-tests-only-signing-key-0123456789abcdef-0123456789abcdef"
            });
        });
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _container.DisposeAsync();
    }
}

/// <summary>
/// Shared fixture so all API tests reuse the same container and database.
/// </summary>
[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
}