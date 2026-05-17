using System.Net;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.Models.DTO.Box;
using StorageLabelsApi.Models.DTO.Item;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class ItemsIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    private async Task<(string UserId, long LocationId, Guid BoxId)> SeedBoxAsync()
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        var response = await client.PostAsJsonAsync("/api/box/",
            new BoxRequest("ITEM-BOX", "Item Test Box", locationId, null, null, null));
        response.EnsureSuccessStatusCode();
        var box = await response.Content.ReadFromJsonAsync<BoxResponse>();
        return (userId, locationId, box!.BoxId);
    }

    [Fact]
    public async Task GetItems_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync($"/api/item/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetItemsByBox_EmptyBox_ReturnsEmptyList()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/item/box/{boxId}/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ItemResponse>>();
        items.ShouldNotBeNull();
        items.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateItem_WithValidData_Returns201()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/item/",
            new ItemRequest(boxId, "Winter Jacket", "Blue parka", null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var item = await response.Content.ReadFromJsonAsync<ItemResponse>();
        item.ShouldNotBeNull();
        item.Name.ShouldBe("Winter Jacket");
        item.Description.ShouldBe("Blue parka");
        item.BoxId.ShouldBe(boxId);
        item.ItemId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task GetItemsByBox_AfterCreate_ReturnsItem()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);
        await client.PostAsJsonAsync("/api/item/",
            new ItemRequest(boxId, "Hammer", null, null, null));

        var response = await client.GetAsync($"/api/item/box/{boxId}/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<List<ItemResponse>>();
        items.ShouldNotBeNull();
        items.Count.ShouldBe(1);
        items[0].Name.ShouldBe("Hammer");
    }

    [Fact]
    public async Task GetItemById_AfterCreate_ReturnsItem()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync("/api/item/",
            new ItemRequest(boxId, "Screwdriver", "Flathead", null, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var getResponse = await client.GetAsync($"/api/item/{created!.ItemId}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var item = await getResponse.Content.ReadFromJsonAsync<ItemResponse>();
        item.ShouldNotBeNull();
        item.ItemId.ShouldBe(created.ItemId);
        item.Name.ShouldBe("Screwdriver");
        item.Description.ShouldBe("Flathead");
    }

    [Fact]
    public async Task GetItemById_NonExistent_Returns404()
    {
        var (userId, _, _) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/item/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateItem_WithValidData_Returns200WithUpdatedFields()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync("/api/item/",
            new ItemRequest(boxId, "Original Name", "Original desc", null, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var updateResponse = await client.PutAsJsonAsync($"/api/item/{created!.ItemId}",
            new ItemRequest(boxId, "Updated Name", "Updated desc", null, null));

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ItemResponse>();
        updated.ShouldNotBeNull();
        updated.Name.ShouldBe("Updated Name");
        updated.Description.ShouldBe("Updated desc");
        updated.ItemId.ShouldBe(created.ItemId);
    }

    [Fact]
    public async Task UpdateItem_NonExistent_Returns404()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PutAsJsonAsync($"/api/item/{Guid.NewGuid()}",
            new ItemRequest(boxId, "Name", null, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteItem_Returns200_AndItemNoLongerFound()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var createResponse = await client.PostAsJsonAsync("/api/item/",
            new ItemRequest(boxId, "To Delete", null, null, null));
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<ItemResponse>();

        var deleteResponse = await client.DeleteAsync($"/api/item/{created!.ItemId}");
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var getResponse = await client.GetAsync($"/api/item/{created.ItemId}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteItem_NonExistent_Returns404()
    {
        var (userId, _, _) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.DeleteAsync($"/api/item/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateItem_WithEmptyName_ReturnsValidationProblem()
    {
        var (userId, _, boxId) = await SeedBoxAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/item/",
            new ItemRequest(boxId, "", null, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
