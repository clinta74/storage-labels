using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Shouldly;
using StorageLabelsApi.Endpoints.EncryptionKeys;
using StorageLabelsApi.Models.DTO.EncryptionKey;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class EncryptionKeysIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    private const string ReadPermission = "read:encryption-keys";
    private const string WritePermission = "write:encryption-keys";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() },
        PropertyNameCaseInsensitive = true
    };

    // ── Auth ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEncryptionKeys_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/admin/encryption-keys");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateEncryptionKey_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "test" });

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ── Create ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEncryptionKey_WithValidRequest_Returns200WithKey()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, WritePermission);

        var response = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "integration test key" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var key = await response.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key.ShouldNotBeNull();
        key.Kid.ShouldBeGreaterThan(0);
        key.Description.ShouldBe("integration test key");
    }

    [Fact]
    public async Task CreateEncryptionKey_WithNoDescription_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, WritePermission);

        var response = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest());

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var key = await response.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key.ShouldNotBeNull();
    }

    // ── Get list ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEncryptionKeys_WithReadPermission_ReturnsEmptyList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission);

        var response = await client.GetAsync("/api/admin/encryption-keys");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var keys = await response.Content.ReadFromJsonAsync<List<EncryptionKeyResponse>>(JsonOptions);
        keys.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetEncryptionKeys_AfterCreate_ReturnsList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission, WritePermission);

        await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "key A" });

        var response = await client.GetAsync("/api/admin/encryption-keys");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var keys = await response.Content.ReadFromJsonAsync<List<EncryptionKeyResponse>>(JsonOptions);
        keys.ShouldNotBeNull();
        keys.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    // ── Stats ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetEncryptionKeyStats_WithUnknownKid_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission);

        var response = await client.GetAsync("/api/admin/encryption-keys/99999/stats");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetEncryptionKeyStats_AfterCreate_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission, WritePermission);

        var createResponse = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "stats test" });
        createResponse.EnsureSuccessStatusCode();
        var key = await createResponse.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key.ShouldNotBeNull();

        var statsResponse = await client.GetAsync($"/api/admin/encryption-keys/{key.Kid}/stats");

        statsResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Activate ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task ActivateEncryptionKey_WithUnknownKid_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, WritePermission);

        var response = await client.PutAsync("/api/admin/encryption-keys/99999/activate", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActivateEncryptionKey_AfterCreate_Returns200WithSuccess()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission, WritePermission);

        var createResponse = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "activate test" });
        createResponse.EnsureSuccessStatusCode();
        var key = await createResponse.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key.ShouldNotBeNull();

        var activateResponse = await client.PutAsync(
            $"/api/admin/encryption-keys/{key.Kid}/activate?autoRotate=false", null);

        activateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await activateResponse.Content.ReadFromJsonAsync<ActivateEncryptionKeyResult>(JsonOptions);
        result.ShouldNotBeNull();
        result.Success.ShouldBeTrue();
    }

    // ── Retire ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task RetireEncryptionKey_WithUnknownKid_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, WritePermission);

        var response = await client.PutAsync("/api/admin/encryption-keys/99999/retire", null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RetireEncryptionKey_AfterActivate_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission, WritePermission);

        var createResponse = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "retire test" });
        createResponse.EnsureSuccessStatusCode();
        var key = await createResponse.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key.ShouldNotBeNull();

        await client.PutAsync($"/api/admin/encryption-keys/{key.Kid}/activate?autoRotate=false", null);

        var retireResponse = await client.PutAsync(
            $"/api/admin/encryption-keys/{key.Kid}/retire", null);

        retireResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Rotations ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRotations_WithReadPermission_ReturnsList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission);

        var response = await client.GetAsync("/api/admin/encryption-keys/rotations");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rotations = await response.Content.ReadFromJsonAsync<List<EncryptionKeyRotationResponse>>(JsonOptions);
        rotations.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetRotationProgress_WithUnknownId_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission);

        var response = await client.GetAsync($"/api/admin/encryption-keys/rotations/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartKeyRotation_WithValidRequest_Returns202WithRotationId()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, ReadPermission, WritePermission);

        // Create and activate first key (source)
        var key1Response = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "source key" });
        key1Response.EnsureSuccessStatusCode();
        var key1 = await key1Response.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key1.ShouldNotBeNull();
        await client.PutAsync($"/api/admin/encryption-keys/{key1.Kid}/activate?autoRotate=false", null);

        // Create second key (target)
        var key2Response = await client.PostAsJsonAsync("/api/admin/encryption-keys",
            new CreateEncryptionKeyRequest { Description = "target key" });
        key2Response.EnsureSuccessStatusCode();
        var key2 = await key2Response.Content.ReadFromJsonAsync<EncryptionKeyResponse>(JsonOptions);
        key2.ShouldNotBeNull();

        var rotateResponse = await client.PostAsJsonAsync("/api/admin/encryption-keys/rotate",
            new StartRotationRequest { FromKeyId = key1.Kid, ToKeyId = key2.Kid, BatchSize = 100 });

        rotateResponse.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var rotationId = await rotateResponse.Content.ReadFromJsonAsync<Guid>();
        rotationId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task CancelRotation_WithUnknownId_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId, WritePermission);

        var response = await client.DeleteAsync($"/api/admin/encryption-keys/rotations/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

