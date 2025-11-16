using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;
using System.IO.Abstractions.TestingHelpers;

namespace StorageLabelsApi.Tests.Services;

public class ImageEncryptionServiceTests
{
    private readonly Mock<ILogger<ImageEncryptionService>> _loggerMock;

    public ImageEncryptionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ImageEncryptionService>>();
    }

    private TestEnvironment CreateTestEnvironment()
    {
        // Create in-memory SQLite database
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<StorageLabelsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new StorageLabelsDbContext(options);
        context.Database.EnsureCreated();

        var fileSystem = new MockFileSystem();
        var service = new ImageEncryptionService(context, _loggerMock.Object, fileSystem);

        return new TestEnvironment(context, service, fileSystem, connection);
    }

    private class TestEnvironment : IDisposable
    {
        public StorageLabelsDbContext context { get; }
        public ImageEncryptionService service { get; }
        public MockFileSystem fileSystem { get; }
        private SqliteConnection connection { get; }

        public TestEnvironment(StorageLabelsDbContext context, ImageEncryptionService service, MockFileSystem fileSystem, SqliteConnection connection)
        {
            this.context = context;
            this.service = service;
            this.fileSystem = fileSystem;
            this.connection = connection;
        }

        public void Dispose()
        {
            context.Dispose();
            connection.Close();
            connection.Dispose();
        }
    }

    [Fact]
    public async Task CreateKeyAsync_CreatesKeyWithCorrectStatus()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        
        // Act
        var key = await env.service.CreateKeyAsync("Test Key", "user123");

        // Assert
        key.Should().NotBeNull();
        key.Status.Should().Be(EncryptionKeyStatus.Created);
        key.Description.Should().Be("Test Key");
        key.CreatedBy.Should().Be("user123");
        key.Version.Should().Be(1);
        key.KeyMaterial.Should().NotBeNull().And.HaveCount(32); // AES-256 = 32 bytes
        key.Algorithm.Should().Be("AES-256-GCM");
    }

    [Fact]
    public async Task CreateKeyAsync_IncrementsVersionNumber()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        await env.service.CreateKeyAsync("Key 1", "user123");
        await env.service.CreateKeyAsync("Key 2", "user123");

        // Act
        var key3 = await env.service.CreateKeyAsync("Key 3", "user123");

        // Assert
        key3.Version.Should().Be(3);
    }

    [Fact]
    public async Task ActivateKeyAsync_ActivatesKeyAndRetiresOldOnes()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key1 = await env.service.CreateKeyAsync("Key 1", "user123");
        var key2 = await env.service.CreateKeyAsync("Key 2", "user123");
        await env.service.ActivateKeyAsync(key1.Kid);
        
        // Act
        var result = await env.service.ActivateKeyAsync(key2.Kid);

        // Assert
        result.Should().BeTrue();
        
        await env.context.Entry(key1).ReloadAsync();
        await env.context.Entry(key2).ReloadAsync();
        
        key1.Status.Should().Be(EncryptionKeyStatus.Retired);
        key2.Status.Should().Be(EncryptionKeyStatus.Active);
        key2.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task ActivateKeyAsync_ReturnsFalseForNonExistentKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        
        // Act
        var result = await env.service.ActivateKeyAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetActiveKeyAsync_ReturnsActiveKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key1 = await env.service.CreateKeyAsync("Key 1", "user123");
        var key2 = await env.service.CreateKeyAsync("Key 2", "user123");
        await env.service.ActivateKeyAsync(key2.Kid);

        // Act
        var activeKey = await env.service.GetActiveKeyAsync();

        // Assert
        activeKey.Should().NotBeNull();
        activeKey!.Kid.Should().Be(key2.Kid);
    }

    [Fact]
    public async Task GetActiveKeyAsync_ReturnsNullWhenNoActiveKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        await env.service.CreateKeyAsync("Key 1", "user123");

        // Act
        var activeKey = await env.service.GetActiveKeyAsync();

        // Assert
        activeKey.Should().BeNull();
    }

    [Fact]
    public async Task EncryptAsync_EncryptsData()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key = await env.service.CreateKeyAsync("Test Key", "user123");
        await env.service.ActivateKeyAsync(key.Kid);
        
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        using var plaintextStream = new MemoryStream(plaintext);

        // Act
        var encrypted = await env.service.EncryptAsync(plaintextStream);

        // Assert
        encrypted.Should().NotBeNull();
        encrypted.EncryptedData.Should().NotBeEquivalentTo(plaintext);
        encrypted.InitializationVector.Should().HaveCount(12); // GCM nonce is 12 bytes
        encrypted.AuthenticationTag.Should().HaveCount(16); // GCM tag is 16 bytes
        encrypted.EncryptionKeyId.Should().Be(key.Kid);
    }

    [Fact]
    public async Task DecryptAsync_DecryptsEncryptedData()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key = await env.service.CreateKeyAsync("Test Key", "user123");
        await env.service.ActivateKeyAsync(key.Kid);
        
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        using var plaintextStream = new MemoryStream(plaintext);
        var encrypted = await env.service.EncryptAsync(plaintextStream);

        // Act
        var decryptedStream = await env.service.DecryptAsync(
            encrypted.EncryptedData, 
            encrypted.EncryptionKeyId,
            encrypted.InitializationVector, 
            encrypted.AuthenticationTag);

        // Assert
        decryptedStream.Should().NotBeNull();
        using var ms = new MemoryStream();
        await decryptedStream.CopyToAsync(ms);
        ms.ToArray().Should().BeEquivalentTo(plaintext);
    }

    [Fact]
    public async Task EncryptExistingImageAsync_EncryptsUnencryptedImage()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key = await env.service.CreateKeyAsync("Test Key", "user123");
        await env.service.ActivateKeyAsync(key.Kid);
        
        var imageData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var storagePath = "/app/data/images/test.jpg";
        
        // Setup mock file system
        env.fileSystem.AddFile(storagePath, new MockFileData(imageData));
        
        var image = new ImageMetadata
        {
            ImageId = Guid.NewGuid(),
            UserId = "user123",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeInBytes = imageData.Length,
            StoragePath = storagePath,
            IsEncrypted = false,
            UploadedAt = DateTime.UtcNow
        };
        
        env.context.Images.Add(image);
        await env.context.SaveChangesAsync();

        // Act
        await env.service.EncryptExistingImageAsync(image, key.Kid);

        // Assert
        image.IsEncrypted.Should().BeTrue();
        image.EncryptionKeyId.Should().Be(key.Kid);
        image.InitializationVector.Should().NotBeNull();
        image.AuthenticationTag.Should().NotBeNull();
        
        // Verify file was updated
        var encryptedData = env.fileSystem.File.ReadAllBytes(storagePath);
        encryptedData.Should().NotBeEquivalentTo(imageData);
    }

    [Fact]
    public async Task ReEncryptImageAsync_ReEncryptsWithNewKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key1 = await env.service.CreateKeyAsync("Key 1", "user123");
        var key2 = await env.service.CreateKeyAsync("Key 2", "user123");
        await env.service.ActivateKeyAsync(key1.Kid);
        
        var imageData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var storagePath = "/app/data/images/test.jpg";
        
        // Setup mock file system
        env.fileSystem.AddFile(storagePath, new MockFileData(imageData));
        
        var image = new ImageMetadata
        {
            ImageId = Guid.NewGuid(),
            UserId = "user123",
            FileName = "test.jpg",
            ContentType = "image/jpeg",
            SizeInBytes = imageData.Length,
            StoragePath = storagePath,
            IsEncrypted = false,
            UploadedAt = DateTime.UtcNow
        };
        
        env.context.Images.Add(image);
        await env.context.SaveChangesAsync();
        
        await env.service.EncryptExistingImageAsync(image, key1.Kid);
        var dataAfterFirstEncryption = env.fileSystem.File.ReadAllBytes(storagePath);

        // Activate key2 so EncryptAsync will use it
        await env.service.ActivateKeyAsync(key2.Kid);

        // Act
        await env.service.ReEncryptImageAsync(image, key2.Kid);

        // Assert
        // Reload the entity to get the updated values
        await env.context.Entry(image).ReloadAsync();
        
        image.IsEncrypted.Should().BeTrue();
        image.EncryptionKeyId.Should().Be(key2.Kid);
        
        var dataAfterReEncryption = env.fileSystem.File.ReadAllBytes(storagePath);
        dataAfterReEncryption.Should().NotBeEquivalentTo(dataAfterFirstEncryption);
    }

    [Fact]
    public async Task RetireKeyAsync_RetiresKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key = await env.service.CreateKeyAsync("Test Key", "user123");
        await env.service.ActivateKeyAsync(key.Kid);

        // Act
        var result = await env.service.RetireKeyAsync(key.Kid);

        // Assert
        result.Should().BeTrue();
        
        await env.context.Entry(key).ReloadAsync();
        
        key.Status.Should().Be(EncryptionKeyStatus.Retired);
        key.RetiredAt.Should().NotBeNull();
    }

    [Fact]
    public async Task RetireKeyAsync_ReturnsFalseForNonExistentKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        
        // Act
        var result = await env.service.RetireKeyAsync(999);

        // Assert
        result.Should().BeFalse();
    }
}
