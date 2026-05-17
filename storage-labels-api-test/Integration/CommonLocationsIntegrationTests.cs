using System.Net;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.Models.DTO.CommonLocation;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class CommonLocationsIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetCommonLocations_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/common-location/");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCommonLocations_WithToken_ReturnsEmptyList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/common-location/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<CommonLocationResponse>>();
        items.ShouldNotBeNull();
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateCommonLocation_WithPermission_Returns201()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, "write:common-locations");

        var response = await client.PostAsJsonAsync("/api/common-location/",
            new CommonLocationRequest("Garage"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CommonLocationResponse>();
        created.ShouldNotBeNull();
        created.Name.ShouldBe("Garage");
        created.CommonLocationId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task CreateCommonLocation_WithoutPermission_Returns403()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/common-location/",
            new CommonLocationRequest("Basement"));

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCommonLocation_AppearsInGetList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, "write:common-locations");

        await client.PostAsJsonAsync("/api/common-location/", new CommonLocationRequest("Attic"));

        var listResponse = await client.GetAsync("/api/common-location/");
        var items = await listResponse.Content.ReadFromJsonAsync<List<CommonLocationResponse>>();
        items.ShouldNotBeNull();
        items.ShouldContain(cl => cl.Name == "Attic");
    }

    [Fact]
    public async Task CreateCommonLocation_WithEmptyName_ReturnsValidationProblem()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, "write:common-locations");

        var response = await client.PostAsJsonAsync("/api/common-location/",
            new CommonLocationRequest(""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteCommonLocation_AfterCreate_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, "write:common-locations");

        var createResponse = await client.PostAsJsonAsync("/api/common-location/",
            new CommonLocationRequest("Shed"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CommonLocationResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/common-location/{created!.CommonLocationId}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteCommonLocation_AfterDelete_NoLongerInList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, "write:common-locations");

        var createResponse = await client.PostAsJsonAsync("/api/common-location/",
            new CommonLocationRequest("Cellar"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CommonLocationResponse>();
        await client.DeleteAsync($"/api/common-location/{created!.CommonLocationId}");

        var listResponse = await client.GetAsync("/api/common-location/");
        var items = await listResponse.Content.ReadFromJsonAsync<List<CommonLocationResponse>>();
        items.ShouldNotBeNull();
        items.ShouldNotContain(cl => cl.CommonLocationId == created.CommonLocationId);
    }

    [Fact]
    public async Task DeleteCommonLocation_NonExistent_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, "write:common-locations");

        var response = await client.DeleteAsync("/api/common-location/99999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCommonLocation_WithoutPermission_Returns403()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var adminClient = CreateAuthenticatedClient(userId, "write:common-locations");
        var userClient = CreateAuthenticatedClient(userId);

        var createResponse = await adminClient.PostAsJsonAsync("/api/common-location/",
            new CommonLocationRequest("Workshop"));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CommonLocationResponse>();

        var deleteResponse = await userClient.DeleteAsync($"/api/common-location/{created!.CommonLocationId}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
