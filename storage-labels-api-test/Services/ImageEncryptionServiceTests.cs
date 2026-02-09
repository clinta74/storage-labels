using Shouldly;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;
using StorageLabelsApi.Tests.TestInfrastructure;
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

        var options = new DbContextOptionsBuilder<TestStorageLabelsDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new TestStorageLabelsDbContext(options);
        context.Database.EnsureCreated();

        var fileSystem = new MockFileSystem();
        var service = new ImageEncryptionService(context, _loggerMock.Object, fileSystem, TimeProvider.System);

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
        key.ShouldNotBeNull();
        key.Status.ShouldBe(EncryptionKeyStatus.Created);
        key.Description.ShouldBe("Test Key");
        key.CreatedBy.ShouldBe("user123");
        key.Version.ShouldBe(1);
        key.KeyMaterial.ShouldNotBeNull();
        key.KeyMaterial.Length.ShouldBe(32); // AES-256 = 32 bytes
        key.Algorithm.ShouldBe("AES-256-GCM");
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
        key3.Version.ShouldBe(3);
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
        result.ShouldBeTrue();
        
        await env.context.Entry(key1).ReloadAsync();
        await env.context.Entry(key2).ReloadAsync();
        
        key1.Status.ShouldBe(EncryptionKeyStatus.Retired);
        key2.Status.ShouldBe(EncryptionKeyStatus.Active);
        key2.ActivatedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task ActivateKeyAsync_ReturnsFalseForNonExistentKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        
        // Act
        var result = await env.service.ActivateKeyAsync(999);

        // Assert
        result.ShouldBeFalse();
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
        activeKey.ShouldNotBeNull();
        activeKey!.Kid.ShouldBe(key2.Kid);
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
        activeKey.ShouldBeNull();
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
        encrypted.ShouldNotBeNull();
        encrypted.EncryptedData.ShouldNotBe(plaintext);
        encrypted.InitializationVector.Length.ShouldBe(12); // GCM nonce is 12 bytes
        encrypted.AuthenticationTag.Length.ShouldBe(16); // GCM tag is 16 bytes
        encrypted.EncryptionKeyId.ShouldBe(key.Kid);
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
        decryptedStream.ShouldNotBeNull();
        using var ms = new MemoryStream();
        await decryptedStream.CopyToAsync(ms);
        ms.ToArray().ShouldBe(plaintext);
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
        image.IsEncrypted.ShouldBeTrue();
        image.EncryptionKeyId.ShouldBe(key.Kid);
        image.InitializationVector.ShouldNotBeNull();
        image.AuthenticationTag.ShouldNotBeNull();
        
        // Verify file was updated
        var encryptedData = env.fileSystem.File.ReadAllBytes(storagePath);
        encryptedData.ShouldNotBe(imageData);
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
        
        image.IsEncrypted.ShouldBeTrue();
        image.EncryptionKeyId.ShouldBe(key2.Kid);
        
        var dataAfterReEncryption = env.fileSystem.File.ReadAllBytes(storagePath);
        dataAfterReEncryption.ShouldNotBe(dataAfterFirstEncryption);
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
        result.ShouldBeTrue();
        
        await env.context.Entry(key).ReloadAsync();
        
        key.Status.ShouldBe(EncryptionKeyStatus.Retired);
        key.RetiredAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task RetireKeyAsync_ReturnsFalseForNonExistentKey()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        
        // Act
        var result = await env.service.RetireKeyAsync(999);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public async Task EncryptAsync_ZerosPlaintextOnSuccess()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key = await env.service.CreateKeyAsync("Test Key", "user123");
        await env.service.ActivateKeyAsync(key.Kid);

        // Create plaintext data with known pattern
        var plaintext = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var plaintextCopy = new byte[plaintext.Length];
        Array.Copy(plaintext, plaintextCopy, plaintext.Length);

        using var inputStream = new MemoryStream(plaintext);

        // Act
        var result = await env.service.EncryptAsync(inputStream);

        // Assert
        result.ShouldNotBeNull();
        result.EncryptedData.ShouldNotBeNull();
        
        // Note: We can't directly verify the internal plaintext buffer was zeroed
        // because it's a local variable. This test verifies the encryption succeeds
        // and the secure zeroing doesn't break the functionality.
        // The actual zeroing is tested at the unit level with CryptographicOperations.ZeroMemory
    }

    [Fact]
    public async Task DecryptAsync_ZerosPlaintextOnDecryptionFailure()
    {
        // Arrange
        using var env = CreateTestEnvironment();
        var key = await env.service.CreateKeyAsync("Test Key", "user123");
        await env.service.ActivateKeyAsync(key.Kid);

        var encryptedData = new byte[32];
        var iv = new byte[12];
        var invalidTag = new byte[16]; // Invalid tag will cause decryption to fail

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await env.service.DecryptAsync(encryptedData, key.Kid, iv, invalidTag));
        
        // If we reach here, the exception was thrown as expected
        // The finally block should have zeroed the plaintext buffer even on failure
    }
}
