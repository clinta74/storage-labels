using System.Net;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.Models.DTO.User;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class UserIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetCurrentUser_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/user/");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithSeededUser_Returns200WithUserData()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/user/");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.ShouldNotBeNull();
        user.UserId.ShouldBe(userId);
        user.FirstName.ShouldBe("Test");
        user.LastName.ShouldBe("User");
    }

    [Fact]
    public async Task CreateUser_WithValidToken_Returns200WithCreatedUser()
    {
        var userId = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.PostAsJsonAsync("/api/user/", new CreateUserRequest("Jane", "Doe"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        user.ShouldNotBeNull();
        user.UserId.ShouldBe(userId);
        user.FirstName.ShouldBe("Jane");
        user.LastName.ShouldBe("Doe");
    }

    [Fact]
    public async Task CreateUser_Twice_Returns409()
    {
        var userId = Guid.NewGuid().ToString();
        var client = CreateAuthenticatedClient(userId);

        await client.PostAsJsonAsync("/api/user/", new CreateUserRequest("Jane", "Doe"));
        var response = await client.PostAsJsonAsync("/api/user/", new CreateUserRequest("Jane", "Doe"));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetUserExists_WithSeededUser_ReturnsTrue()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/user/exists");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var exists = await response.Content.ReadFromJsonAsync<bool>();
        exists.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserExists_WithUnknownUser_ReturnsFalse()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/user/exists");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var exists = await response.Content.ReadFromJsonAsync<bool>();
        exists.ShouldBeFalse();
    }

    [Fact]
    public async Task GetUserExists_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/user/exists");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserPreferences_WithSeededUser_Returns200WithDefaults()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/user/preferences");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var prefs = await response.Content.ReadFromJsonAsync<UserPreferencesResponse>();
        prefs.ShouldNotBeNull();
        prefs.Theme.ShouldBe("light");
        prefs.ShowImages.ShouldBeTrue();
    }

    [Fact]
    public async Task GetUserPreferences_WithUnknownUser_Returns404()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.GetAsync("/api/user/preferences");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateUserPreferences_WithValidData_Returns200WithUpdatedPrefs()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var updated = new UserPreferencesResponse { Theme = "dark", ShowImages = false, CodeColorPattern = "blue" };
        var response = await client.PutAsJsonAsync("/api/user/preferences", updated);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var prefs = await response.Content.ReadFromJsonAsync<UserPreferencesResponse>();
        prefs.ShouldNotBeNull();
        prefs.Theme.ShouldBe("dark");
        prefs.ShowImages.ShouldBeFalse();
        prefs.CodeColorPattern.ShouldBe("blue");
    }

    [Fact]
    public async Task UpdateUserPreferences_PersistsAcrossRequests()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        await client.PutAsJsonAsync("/api/user/preferences",
            new UserPreferencesResponse { Theme = "dark", ShowImages = true, CodeColorPattern = "" });

        var getResponse = await client.GetAsync("/api/user/preferences");
        var prefs = await getResponse.Content.ReadFromJsonAsync<UserPreferencesResponse>();
        prefs.ShouldNotBeNull();
        prefs.Theme.ShouldBe("dark");
    }

    [Fact]
    public async Task UpdateUserPreferences_WithUnknownUser_Returns404()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid().ToString());

        var response = await client.PutAsJsonAsync("/api/user/preferences",
            new UserPreferencesResponse { Theme = "dark", ShowImages = false, CodeColorPattern = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
