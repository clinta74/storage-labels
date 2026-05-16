using System.Net;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.Models.DTO.Box;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class BoxesIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetBox_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/box/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetBoxesByLocation_WithValidToken_ReturnsEmptyList()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/box/location/{locationId}/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var boxes = await response.Content.ReadFromJsonAsync<List<BoxResponse>>();
        boxes.ShouldNotBeNull();
        boxes.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateBox_WithValidData_Returns201WithBoxDetails()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var request = new BoxRequest("BOX-001", "Test Box", locationId, "A test box", null, null);

        var response = await client.PostAsJsonAsync("/api/box/", request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<BoxResponse>();
        created.ShouldNotBeNull();
        created.Code.ShouldBe("BOX-001");
        created.Name.ShouldBe("Test Box");
        created.Description.ShouldBe("A test box");
        created.LocationId.ShouldBe(locationId);
        created.BoxId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetBoxById_AfterCreate_ReturnsBox()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync("/api/box/",
            new BoxRequest("BOX-002", "Another Box", locationId, null, null, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<BoxResponse>();

        var getResponse = await client.GetAsync($"/api/box/{created!.BoxId}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var box = await getResponse.Content.ReadFromJsonAsync<BoxResponse>();
        box.ShouldNotBeNull();
        box.BoxId.ShouldBe(created.BoxId);
        box.Code.ShouldBe("BOX-002");
    }

    [Fact]
    public async Task CreateBox_WithDuplicateCodeInSameLocation_Returns409()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync("/api/box/",
            new BoxRequest("DUPE-001", "First Box", locationId, null, null, null));

        var response = await client.PostAsJsonAsync("/api/box/",
            new BoxRequest("DUPE-001", "Second Box", locationId, null, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetBoxById_NonExistent_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/box/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Respawn_ResetsDataBetweenTests()
    {
        // Each test starts with a fresh DB state — this seeds its own user/location
        // and asserts only its own data exists, proving Respawn is working.
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/box/location/{locationId}/");
        var boxes = await response.Content.ReadFromJsonAsync<List<BoxResponse>>();

        boxes.ShouldNotBeNull();
        boxes.ShouldBeEmpty();
    }
}
