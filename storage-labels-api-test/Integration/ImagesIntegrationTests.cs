using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Image;
using StorageLabelsApi.Tests.TestInfrastructure;

namespace StorageLabelsApi.Tests.Integration;

public class ImagesIntegrationTests(IntegrationDatabaseFixture fixture)
    : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task GetUserImages_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/images");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUserImages_WithValidToken_ReturnsEmptyList()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync("/api/images");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var images = await response.Content.ReadFromJsonAsync<List<ImageMetadataResponse>>();
        images.ShouldNotBeNull();
        images.ShouldBeEmpty();
    }

    [Fact]
    public async Task UploadImage_WithoutToken_Returns401()
    {
        var client = Fixture.Factory.CreateClient();
        using var content = BuildJpegFormContent("test.jpg");

        var response = await client.PostAsync("/api/images", content);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UploadImage_WithValidJpeg_Returns200WithMetadata()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        using var content = BuildJpegFormContent("photo.jpg");

        var response = await client.PostAsync("/api/images", content);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var metadata = await response.Content.ReadFromJsonAsync<ImageMetadata>();
        metadata.ShouldNotBeNull();
        metadata.ImageId.ShouldNotBe(Guid.Empty);
        metadata.FileName.ShouldBe("photo.jpg");
        metadata.ContentType.ShouldBe("image/jpeg");
    }

    [Fact]
    public async Task UploadImage_WithNonJpeg_Returns400()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        using var formContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]); // PNG magic bytes
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        formContent.Add(fileContent, "file", "photo.png");

        var response = await client.PostAsync("/api/images", formContent);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUserImages_AfterUpload_ReturnsUploadedImage()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        using var content = BuildJpegFormContent("photo.jpg");

        var uploadResponse = await client.PostAsync("/api/images", content);
        uploadResponse.EnsureSuccessStatusCode();

        var listResponse = await client.GetAsync("/api/images");
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var images = await listResponse.Content.ReadFromJsonAsync<List<ImageMetadataResponse>>();
        images.ShouldNotBeNull();
        images.Count.ShouldBeGreaterThanOrEqualTo(1);
        images.ShouldContain(i => i.FileName == "photo.jpg");
    }

    [Fact]
    public async Task GetImageFile_WithUnknownId_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.GetAsync($"/api/images/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetImageFile_AfterUpload_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        using var content = BuildJpegFormContent("photo.jpg");

        var uploadResponse = await client.PostAsync("/api/images", content);
        uploadResponse.EnsureSuccessStatusCode();
        var metadata = await uploadResponse.Content.ReadFromJsonAsync<ImageMetadata>();
        metadata.ShouldNotBeNull();

        var getResponse = await client.GetAsync($"/api/images/{metadata.ImageId}");

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteImage_WithUnknownId_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);

        var response = await client.DeleteAsync($"/api/images/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteImage_AfterUpload_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        using var content = BuildJpegFormContent("photo.jpg");

        var uploadResponse = await client.PostAsync("/api/images", content);
        uploadResponse.EnsureSuccessStatusCode();
        var metadata = await uploadResponse.Content.ReadFromJsonAsync<ImageMetadata>();
        metadata.ShouldNotBeNull();

        var deleteResponse = await client.DeleteAsync($"/api/images/{metadata.ImageId}");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteImage_AfterDelete_Returns404()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        using var content = BuildJpegFormContent("photo.jpg");

        var uploadResponse = await client.PostAsync("/api/images", content);
        uploadResponse.EnsureSuccessStatusCode();
        var metadata = await uploadResponse.Content.ReadFromJsonAsync<ImageMetadata>();
        metadata.ShouldNotBeNull();

        await client.DeleteAsync($"/api/images/{metadata.ImageId}");
        var secondDelete = await client.DeleteAsync($"/api/images/{metadata.ImageId}");

        secondDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ForceDeleteImage_AfterUpload_Returns200()
    {
        var (userId, _) = await SeedTestUserWithLocationAsync();
        var client = CreateAuthenticatedClient(userId);
        using var content = BuildJpegFormContent("photo.jpg");

        var uploadResponse = await client.PostAsync("/api/images", content);
        uploadResponse.EnsureSuccessStatusCode();
        var metadata = await uploadResponse.Content.ReadFromJsonAsync<ImageMetadata>();
        metadata.ShouldNotBeNull();

        var deleteResponse = await client.DeleteAsync($"/api/images/{metadata.ImageId}/force");

        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // Builds minimal valid JPEG multipart form data
    private static MultipartFormDataContent BuildJpegFormContent(string fileName)
    {
        // Minimal valid JPEG header (SOI + EOI markers)
        byte[] minimalJpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49,
            0x46, 0x00, 0x01, 0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xD9];

        var formContent = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(minimalJpeg);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        formContent.Add(fileContent, "file", fileName);
        return formContent;
    }
}
