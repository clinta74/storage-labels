using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Tests.TestInfrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Fixed test JWT credentials — must not match JwtSettings.IsSecretPlaceholder()
    public const string TestJwtSecret = "integration-tests-signing-key-min-32chars!xyz";
    public const string TestJwtIssuer = "storage-labels-api";
    public const string TestJwtAudience = "storage-labels-ui";

    private static string GetEnvOrDefault(string name, string defaultValue) =>
        Environment.GetEnvironmentVariable(name) ?? defaultValue;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["POSTGRES_HOST"]     = GetEnvOrDefault("POSTGRES_HOST", "localhost"),
                ["POSTGRES_PORT"]     = GetEnvOrDefault("POSTGRES_PORT", "5433"),
                ["POSTGRES_DATABASE"] = GetEnvOrDefault("POSTGRES_DATABASE", "StorageLabelsTest"),
                ["POSTGRES_USERNAME"] = GetEnvOrDefault("POSTGRES_USERNAME", "storage_test_user"),
                ["POSTGRES_PASSWORD"] = GetEnvOrDefault("POSTGRES_PASSWORD", "storage_test_password"),
                ["POSTGRES_SSL_MODE"] = "Disable",
                ["Jwt:Secret"]        = TestJwtSecret,
                ["Jwt:Issuer"]        = TestJwtIssuer,
                ["Jwt:Audience"]      = TestJwtAudience,
                ["Authentication:Mode"] = "Local",
                // Generous rate limits so tests don't get throttled
                ["RateLimit:Global:PermitLimit"] = "10000",
                ["RateLimit:Auth:PermitLimit"]   = "10000",
                ["RateLimit:Search:TokenLimit"]  = "10000",
                ["RateLimit:Images:PermitLimit"] = "1000",
            });
        });
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();
        await db.Database.MigrateAsync();
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
    }
}
