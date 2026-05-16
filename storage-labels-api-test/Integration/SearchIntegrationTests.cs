using System.Net;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.Models.DTO.Box;
using StorageLabelsApi.Models.DTO.Search;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class SearchIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    private async Task<(string UserId, long LocationId, BoxResponse Box)> SeedBoxWithCodeAsync(string code, string name)
    {
        var (userId, locationId) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        var response = await client.PostAsJsonAsync("/api/box/",
            new BoxRequest(code, name, locationId, null, null, null));
        response.EnsureSuccessStatusCode();
        var box = await response.Content.ReadFromJsonAsync<BoxResponse>();
        return (userId, locationId, box!);
    }

    [Fact]
    public async Task SearchByQrCode_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/search/qrcode/SOME-CODE");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchByQrCode_WithMatchingCode_Returns200WithResult()
    {
        var (userId, _, box) = await SeedBoxWithCodeAsync("QR-SRCH-001", "QR Search Box");
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/search/qrcode/{box.Code}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<SearchResultResponse>();
        result.ShouldNotBeNull();
        result.Type.ShouldBe("box");
        result.BoxCode.ShouldBe("QR-SRCH-001");
        result.BoxName.ShouldBe("QR Search Box");
    }

    [Fact]
    public async Task SearchByQrCode_WithNonExistentCode_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/search/qrcode/DOES-NOT-EXIST-XYZ");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchByQrCode_OtherUsersBox_Returns404()
    {
        var (_, _, box) = await SeedBoxWithCodeAsync("QR-PRIV-001", "Private Box");
        var (otherUserId, _) = await SeedTestUserWithLocationAsync();
        var otherClient = CreateAuthenticatedClient(otherUserId);

        var response = await otherClient.GetAsync($"/api/search/qrcode/{box.Code}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SearchBoxesAndItems_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/search?query=test");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchBoxesAndItems_WithValidQuery_Returns200WithList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/search?query=test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResultResponse>>();
        results.ShouldNotBeNull();
    }

    [Fact]
    public async Task SearchBoxesAndItems_EmptyDatabase_ReturnsEmptyList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/search?query=zzz-no-match-xyz-abc");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var results = await response.Content.ReadFromJsonAsync<List<SearchResultResponse>>();
        results.ShouldNotBeNull();
        results.ShouldBeEmpty();
    }

    [Fact]
    public async Task SearchBoxesAndItems_TotalCountHeaderPresent()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/search?query=test");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Headers.Contains("x-total-count").ShouldBeTrue();
    }
}
