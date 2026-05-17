using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Tests.TestInfrastructure;

public class IntegrationTestWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Fixed test JWT credentials — must not match JwtSettings.IsSecretPlaceholder()
    public const string TestJwtSecret = "integration-tests-signing-key-min-32chars!xyz";
    public const string TestJwtIssuer = "storage-labels-api";
    public const string TestJwtAudience = "storage-labels-ui";

    private readonly string _imageStoragePath =
        Path.Combine(Path.GetTempPath(), "storage-labels-tests", Guid.NewGuid().ToString());

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
                // Use a writable temp directory for image storage in tests
                ["IMAGE_STORAGE_PATH"] = _imageStoragePath,
                // Generous rate limits so tests don't get throttled
                ["RateLimit:Global:PermitLimit"] = "10000",
                ["RateLimit:Auth:PermitLimit"]   = "10000",
                ["RateLimit:Search:TokenLimit"]  = "10000",
                ["RateLimit:Images:PermitLimit"] = "1000",
            });
        });

        // The JWT signing key is captured at Program.cs startup time from configuration.
        // Because WebApplicationBuilder reads config before ConfigureAppConfiguration callbacks
        // run, the app may generate a random secret (from the placeholder in appsettings.json)
        // before our test override is applied. PostConfigure guarantees the test key is used
        // for validation regardless of startup order.
        builder.ConfigureServices(services =>
        {
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtSecret));
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = signingKey;
                options.TokenValidationParameters.ValidIssuer = TestJwtIssuer;
                options.TokenValidationParameters.ValidAudience = TestJwtAudience;
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
        if (Directory.Exists(_imageStoragePath))
            Directory.Delete(_imageStoragePath, recursive: true);
    }
}
