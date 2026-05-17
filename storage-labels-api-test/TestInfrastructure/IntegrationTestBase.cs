using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Tests.TestInfrastructure;

[Collection("Integration")]
[Trait("Category", "Integration")]
public abstract class IntegrationTestBase(IntegrationDatabaseFixture fixture) : IAsyncLifetime
{
    protected IntegrationDatabaseFixture Fixture { get; } = fixture;

    public async Task InitializeAsync() => await Fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    /// <summary>
    /// Creates an HttpClient with a valid JWT bearer token for the given user and permissions.
    /// </summary>
    protected HttpClient CreateAuthenticatedClient(string userId, params string[] permissions)
    {
        var token = JwtTokenHelper.GenerateToken(
            userId,
            IntegrationTestWebAppFactory.TestJwtSecret,
            IntegrationTestWebAppFactory.TestJwtIssuer,
            IntegrationTestWebAppFactory.TestJwtAudience,
            permissions);

        var client = Fixture.Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates a scoped service provider for direct database access.
    /// </summary>
    protected IServiceScope CreateDbScope() => Fixture.Factory.Services.CreateScope();

    /// <summary>
    /// Seeds a test user with a location and returns the userId and locationId.
    /// The authenticated client returned from <see cref="CreateAuthenticatedClient"/> uses
    /// this userId to pass the UserExistsFilter.
    /// </summary>
    protected async Task<(string UserId, long LocationId)> SeedTestUserWithLocationAsync(
        AccessLevels accessLevel = AccessLevels.Owner)
    {
        var userId = Guid.NewGuid().ToString();
        var now = DateTimeOffset.UtcNow;

        using var scope = CreateDbScope();
        var db = scope.ServiceProvider.GetRequiredService<StorageLabelsDbContext>();

        db.Users.Add(new User(userId, "Test", "User", $"{userId}@test.com", now));
        var location = db.Locations.Add(new Location(
            LocationId: 0,
            Name: "Test Location",
            Created: now,
            Updated: now)).Entity;

        await db.SaveChangesAsync();

        db.UserLocations.Add(new UserLocation(userId, location.LocationId, accessLevel, now, now));
        await db.SaveChangesAsync();

        return (userId, location.LocationId);
    }
}
