using System.Net;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.Models.DTO.Box;
using StorageLabelsApi.Models.DTO.Location;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class LocationsIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetLocations_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/location/");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetLocations_WithValidToken_ReturnsOwnedLocations()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/location/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var locations = await response.Content.ReadFromJsonAsync<List<LocationResponse>>();
        locations.ShouldNotBeNull();
        locations.ShouldContain(l => l.LocationId == locationId);
    }

    [Fact]
    public async Task CreateLocation_WithValidData_Returns201WithLocation()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/location/", new LocationRequest("New Storage Room"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var location = await response.Content.ReadFromJsonAsync<LocationResponse>();
        location.ShouldNotBeNull();
        location.Name.ShouldBe("New Storage Room");
        location.LocationId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateLocation_AppearsInGetLocations()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync("/api/location/", new LocationRequest("Warehouse"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<LocationResponse>();

        var listResponse = await client.GetAsync("/api/location/");
        var locations = await listResponse.Content.ReadFromJsonAsync<List<LocationResponse>>();
        locations.ShouldNotBeNull();
        locations.ShouldContain(l => l.LocationId == created!.LocationId);
    }

    [Fact]
    public async Task GetLocation_ById_ReturnsLocation()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/location/{locationId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var location = await response.Content.ReadFromJsonAsync<LocationResponse>();
        location.ShouldNotBeNull();
        location.LocationId.ShouldBe(locationId);
        location.Name.ShouldBe("Test Location");
    }

    [Fact]
    public async Task GetLocation_NonExistent_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/location/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLocation_OtherUsersLocation_Returns404()
    {
        var (_, locationId) = await SeedTestUserWithLocationAsync();
        var (otherUserId, _) = await SeedTestUserWithLocationAsync();
        var otherClient = CreateAuthenticatedClient(otherUserId);

        var response = await otherClient.GetAsync($"/api/location/{locationId}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLocation_WithValidData_Returns200WithUpdatedName()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PutAsJsonAsync($"/api/location/{locationId}", new LocationRequest("Updated Name"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var location = await response.Content.ReadFromJsonAsync<LocationResponse>();
        location.ShouldNotBeNull();
        location.Name.ShouldBe("Updated Name");
        location.LocationId.ShouldBe(locationId);
    }

    [Fact]
    public async Task UpdateLocation_NonExistent_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PutAsJsonAsync("/api/location/999999", new LocationRequest("Name"));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteLocation_WithNoBoxes_Returns200()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.DeleteAsync($"/api/location/{locationId}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteLocation_WithBoxes_WithoutForce_ReturnsValidationProblem()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        await client.PostAsJsonAsync("/api/box/",
            new BoxRequest("BLK-001", "Blocking Box", locationId, null, null, null));

        var response = await client.DeleteAsync($"/api/location/{locationId}");

        response.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DeleteLocation_WithBoxes_WithForce_Returns200()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        await client.PostAsJsonAsync("/api/box/",
            new BoxRequest("FRC-001", "Forced Box", locationId, null, null, null));

        var response = await client.DeleteAsync($"/api/location/{locationId}?force=true");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteLocation_NonExistent_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.DeleteAsync("/api/location/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
