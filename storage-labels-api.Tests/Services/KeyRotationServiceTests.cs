using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace StorageLabelsApi.Tests.Services;

/// <summary>
/// A mock file system that introduces delays to simulate slow file I/O operations
/// </summary>
public class DelayedMockFileSystem : MockFileSystem
{
    private readonly int _delayMs;

    public DelayedMockFileSystem(int delayMs = 0) : base()
    {
        _delayMs = delayMs;
    }

    public override IFile File => new DelayedMockFile(this, _delayMs);

    private class DelayedMockFile : MockFile
    {
        private readonly int _delayMs;

        public DelayedMockFile(IMockFileDataAccessor mockFileDataAccessor, int delayMs) 
            : base(mockFileDataAccessor)
        {
            _delayMs = delayMs;
        }

        public override async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancellationToken);
            }
            return await base.ReadAllBytesAsync(path, cancellationToken);
        }

        public override async Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancellationToken);
            }
            await base.WriteAllBytesAsync(path, bytes, cancellationToken);
        }
    }
}

public class KeyRotationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly StorageLabelsDbContext _context;
    private readonly Mock<ILogger<KeyRotationService>> _loggerMock;
    private readonly Mock<ILogger<ImageEncryptionService>> _encryptionLoggerMock;
    private readonly Mock<IRotationProgressNotifier> _progressNotifierMock;
    private readonly MockFileSystem _fileSystem;
    private readonly ImageEncryptionService _encryptionService;
    private readonly KeyRotationService _service;
    private readonly ServiceProvider _serviceProvider;

    public KeyRotationServiceTests()
    {
        // Create in-memory SQLite database
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<StorageLabelsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new StorageLabelsDbContext(options);
        _context.Database.EnsureCreated();

        _loggerMock = new Mock<ILogger<KeyRotationService>>();
        _encryptionLoggerMock = new Mock<ILogger<ImageEncryptionService>>();
        _progressNotifierMock = new Mock<IRotationProgressNotifier>();
        _fileSystem = new MockFileSystem();
        _encryptionService = new ImageEncryptionService(_context, _encryptionLoggerMock.Object, _fileSystem, TimeProvider.System);

        // Setup service provider for scoped services that doesn't get disposed
        var services = new ServiceCollection();
        services.AddScoped(_ => _context);
        services.AddScoped<IImageEncryptionService>(_ => _encryptionService);
        _serviceProvider = services.BuildServiceProvider();

        // Create a mock scope that returns our services but doesn't dispose the provider
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(_serviceProvider);
        
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);

        _service = new KeyRotationService(
            scopeFactoryMock.Object,
            _loggerMock.Object,
            _progressNotifierMock.Object,
            TimeProvider.System);
    }

    private ImageMetadata CreateUnencryptedImage(string userId = "user123")
    {
        var imageData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var fileName = $"test-{Guid.NewGuid()}.jpg";
        var storagePath = $"/app/data/images/{fileName}";
        
        // Setup mock file system
        _fileSystem.AddFile(storagePath, new MockFileData(imageData));
        
        var image = new ImageMetadata
        {
            ImageId = Guid.NewGuid(),
            UserId = userId,
            FileName = fileName,
            ContentType = "image/jpeg",
            SizeInBytes = imageData.Length,
            StoragePath = storagePath,
            IsEncrypted = false,
            UploadedAt = DateTime.UtcNow
        };
        
        _context.Images.Add(image);
        _context.SaveChanges();
        
        return image;
    }

    [Fact]
    public async Task StartRotationAsync_WithNullFromKeyId_StartsUnencryptedMigration()
    {
        // Arrange
        var key = await _encryptionService.CreateKeyAsync("Target Key", "user123");
        await _encryptionService.ActivateKeyAsync(key.Kid);
        
        // Add unencrypted images
        var image1 = CreateUnencryptedImage();
        var image2 = CreateUnencryptedImage();

        var options = new RotationOptions(
            FromKeyId: null,
            ToKeyId: key.Kid,
            BatchSize: 100,
            InitiatedBy: "user123",
            IsAutomatic: false
        );

        // Act
        var rotation = await _service.StartRotationAsync(options);

        // Assert
        rotation.Should().NotBeNull();
        rotation.FromKeyId.Should().BeNull();
        rotation.ToKeyId.Should().Be(key.Kid);
        rotation.Status.Should().Be(RotationStatus.InProgress);
        rotation.TotalImages.Should().Be(2);
    }

    [Fact]
    public async Task StartRotationAsync_WithFromKeyId_StartsKeyRotation()
    {
        // Arrange
        var key1 = await _encryptionService.CreateKeyAsync("Key 1", "user123");
        var key2 = await _encryptionService.CreateKeyAsync("Key 2", "user123");
        await _encryptionService.ActivateKeyAsync(key1.Kid);
        
        // Add encrypted images
        var image1 = CreateUnencryptedImage();
        var image2 = CreateUnencryptedImage();
        
        await _encryptionService.EncryptExistingImageAsync(image1, key1.Kid);
        await _encryptionService.EncryptExistingImageAsync(image2, key1.Kid);

        var options = new RotationOptions(
            FromKeyId: key1.Kid,
            ToKeyId: key2.Kid,
            BatchSize: 100,
            InitiatedBy: "user123",
            IsAutomatic: false
        );

        // Act
        var rotation = await _service.StartRotationAsync(options);

        // Assert
        rotation.Should().NotBeNull();
        rotation.FromKeyId.Should().Be(key1.Kid);
        rotation.ToKeyId.Should().Be(key2.Kid);
        rotation.Status.Should().Be(RotationStatus.InProgress);
        rotation.TotalImages.Should().Be(2);
    }

    [Fact]
    public async Task StartRotationAsync_ThrowsWhenTargetKeyNotFound()
    {
        // Arrange
        var options = new RotationOptions(
            FromKeyId: null,
            ToKeyId: 999,
            BatchSize: 100,
            InitiatedBy: "user123",
            IsAutomatic: false
        );

        // Act
        var act = async () => await _service.StartRotationAsync(options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Target key 999 not found*");
    }

    [Fact]
    public async Task StartRotationAsync_ThrowsWhenTargetKeyNotActive()
    {
        // Arrange
        var key = await _encryptionService.CreateKeyAsync("Inactive Key", "user123");
        
        var options = new RotationOptions(
            FromKeyId: null,
            ToKeyId: key.Kid,
            BatchSize: 100,
            InitiatedBy: "user123",
            IsAutomatic: false
        );

        // Act
        var act = async () => await _service.StartRotationAsync(options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Target key {key.Kid} is not active*");
    }

    [Fact]
    public async Task StartRotationAsync_ThrowsWhenSourceKeyNotFound()
    {
        // Arrange
        var targetKey = await _encryptionService.CreateKeyAsync("Target Key", "user123");
        await _encryptionService.ActivateKeyAsync(targetKey.Kid);
        
        var options = new RotationOptions(
            FromKeyId: 999,
            ToKeyId: targetKey.Kid,
            BatchSize: 100,
            InitiatedBy: "user123",
            IsAutomatic: false
        );

        // Act
        var act = async () => await _service.StartRotationAsync(options);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Source key 999 not found*");
    }

    [Fact]
    public async Task GetRotationProgressAsync_ReturnsProgressForExistingRotation()
    {
        // Arrange
        var key = await _encryptionService.CreateKeyAsync("Test Key", "user123");
        await _encryptionService.ActivateKeyAsync(key.Kid);
        
        var rotation = new EncryptionKeyRotation
        {
            FromKeyId = null,
            ToKeyId = key.Kid,
            Status = RotationStatus.InProgress,
            TotalImages = 10,
            ProcessedImages = 5,
            FailedImages = 1,
            InitiatedBy = "user123"
        };
        
        _context.EncryptionKeyRotations.Add(rotation);
        await _context.SaveChangesAsync();

        // Act
        var progress = await _service.GetRotationProgressAsync(rotation.Id);

        // Assert
        progress.Should().NotBeNull();
        progress!.RotationId.Should().Be(rotation.Id);
        progress.Status.Should().Be(RotationStatus.InProgress);
        progress.TotalImages.Should().Be(10);
        progress.ProcessedImages.Should().Be(5);
        progress.FailedImages.Should().Be(1);
    }

    [Fact]
    public async Task GetRotationProgressAsync_ReturnsNullForNonExistentRotation()
    {
        // Act
        var progress = await _service.GetRotationProgressAsync(Guid.NewGuid());

        // Assert
        progress.Should().BeNull();
    }

    [Fact]
    public async Task GetRotationProgressAsync_TracksProgressDuringRotation()
    {
        // Arrange - Create a separate DbContext for this test to avoid concurrency issues
        var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        
        var options = new DbContextOptionsBuilder<StorageLabelsDbContext>()
            .UseSqlite(testConnection)
            .Options;
        
        var testContext = new StorageLabelsDbContext(options);
        testContext.Database.EnsureCreated();
        
        var delayedFileSystem = new DelayedMockFileSystem(delayMs: 400); // 400ms delay per file operation
        var delayedEncryptionService = new ImageEncryptionService(testContext, _encryptionLoggerMock.Object, delayedFileSystem, TimeProvider.System);
        
        // Setup service provider with delayed encryption service
        var services = new ServiceCollection();
        services.AddScoped(_ => {
            // Reuse the same connection so all contexts share the same in-memory database  
            var scopedOptions = new DbContextOptionsBuilder<StorageLabelsDbContext>()
                .UseSqlite(testConnection)
                .Options;
            return new StorageLabelsDbContext(scopedOptions);
        });
        services.AddScoped<IImageEncryptionService>(_ => delayedEncryptionService);
        var delayedServiceProvider = services.BuildServiceProvider();
        
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(delayedServiceProvider);
        scopeMock.Setup(x => x.Dispose()).Callback(() => { });
        
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        
        var delayedRotationService = new KeyRotationService(
            scopeFactoryMock.Object,
            _loggerMock.Object,
            _progressNotifierMock.Object,
            TimeProvider.System);
        
        var key = await delayedEncryptionService.CreateKeyAsync("Test Key", "user123");
        await delayedEncryptionService.ActivateKeyAsync(key.Kid);
        
        // Create images with the delayed file system
        for (int i = 0; i < 5; i++)
        {
            var imageData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var fileName = $"test-{Guid.NewGuid()}.jpg";
            var storagePath = $"/app/data/images/{fileName}";
            
            delayedFileSystem.AddFile(storagePath, new MockFileData(imageData));
            
            var image = new ImageMetadata
            {
                ImageId = Guid.NewGuid(),
                UserId = "user123",
                FileName = fileName,
                ContentType = "image/jpeg",
                SizeInBytes = imageData.Length,
                StoragePath = storagePath,
                IsEncrypted = false,
                UploadedAt = DateTime.UtcNow
            };
            
            testContext.Images.Add(image);
        }
        await testContext.SaveChangesAsync();
        
        var options2 = new RotationOptions(
            FromKeyId: null,
            ToKeyId: key.Kid,
            BatchSize: 1, // Process one at a time
            InitiatedBy: "user123",
            IsAutomatic: false
        );
        
        var rotation = await delayedRotationService.StartRotationAsync(options2);
        
        // Give the background task time to start and process at least one image
        // Each image takes ~800ms (2 file operations: read + write at 400ms each)
        await Task.Delay(1000);

        // Act
        var progress = await delayedRotationService.GetRotationProgressAsync(rotation.Id);

        // Assert
        progress.Should().NotBeNull();
        progress!.RotationId.Should().Be(rotation.Id);
        progress.TotalImages.Should().Be(5);
        progress.Status.Should().BeOneOf(RotationStatus.InProgress, RotationStatus.Completed);
        
        // Should have processed at least one but not all (unless it completed)
        if (progress.Status == RotationStatus.InProgress)
        {
            progress.ProcessedImages.Should().BeGreaterThan(0).And.BeLessThan(5);
        }
        
        // Clean up - cancel to stop background processing
        await delayedRotationService.CancelRotationAsync(rotation.Id);
        await Task.Delay(200);
        delayedServiceProvider.Dispose();
        testContext.Dispose();
        testConnection.Close();
        testConnection.Dispose();
    }

    [Fact]
    public async Task CancelRotationAsync_ReturnsFalseForNonExistentRotation()
    {
        // Act
        var result = await _service.CancelRotationAsync(Guid.NewGuid());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CancelRotationAsync_CancelsInProgressRotation()
    {
        // Arrange - Create a separate DbContext for this test to avoid concurrency issues
        var testConnection = new SqliteConnection("DataSource=:memory:");
        testConnection.Open();
        
        var options = new DbContextOptionsBuilder<StorageLabelsDbContext>()
            .UseSqlite(testConnection)
            .Options;
        
        var testContext = new StorageLabelsDbContext(options);
        testContext.Database.EnsureCreated();
        
        var delayedFileSystem = new DelayedMockFileSystem(delayMs: 300); // 300ms delay per file operation
        var delayedEncryptionService = new ImageEncryptionService(testContext, _encryptionLoggerMock.Object, delayedFileSystem, TimeProvider.System);
        
        // Setup service provider with delayed encryption service
        var services = new ServiceCollection();
        services.AddScoped(_ => {
            // Reuse the same connection so all contexts share the same in-memory database
            var scopedOptions = new DbContextOptionsBuilder<StorageLabelsDbContext>()
                .UseSqlite(testConnection)
                .Options;
            return new StorageLabelsDbContext(scopedOptions);
        });
        services.AddScoped<IImageEncryptionService>(_ => delayedEncryptionService);
        var delayedServiceProvider = services.BuildServiceProvider();
        
        var scopeMock = new Mock<IServiceScope>();
        scopeMock.Setup(x => x.ServiceProvider).Returns(delayedServiceProvider);
        scopeMock.Setup(x => x.Dispose()).Callback(() => { }); // Prevent early disposal
        
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        scopeFactoryMock.Setup(x => x.CreateScope()).Returns(scopeMock.Object);
        
        var delayedRotationService = new KeyRotationService(
            scopeFactoryMock.Object,
            _loggerMock.Object,
            _progressNotifierMock.Object,
            TimeProvider.System);
        
        var key = await delayedEncryptionService.CreateKeyAsync("Test Key", "user123");
        await delayedEncryptionService.ActivateKeyAsync(key.Kid);
        
        // Create images with the delayed file system
        for (int i = 0; i < 5; i++)
        {
            var imageData = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            var fileName = $"test-{Guid.NewGuid()}.jpg";
            var storagePath = $"/app/data/images/{fileName}";
            
            delayedFileSystem.AddFile(storagePath, new MockFileData(imageData));
            
            var image = new ImageMetadata
            {
                ImageId = Guid.NewGuid(),
                UserId = "user123",
                FileName = fileName,
                ContentType = "image/jpeg",
                SizeInBytes = imageData.Length,
                StoragePath = storagePath,
                IsEncrypted = false,
                UploadedAt = DateTime.UtcNow
            };
            
            testContext.Images.Add(image);
        }
        await testContext.SaveChangesAsync();
        
        var options2 = new RotationOptions(
            FromKeyId: null,
            ToKeyId: key.Kid,
            BatchSize: 2,
            InitiatedBy: "user123",
            IsAutomatic: false
        );
        
        var rotation = await delayedRotationService.StartRotationAsync(options2);
        
        // Give the background task time to start processing (but not finish all 5 images)
        await Task.Delay(200);

        // Act
        var result = await delayedRotationService.CancelRotationAsync(rotation.Id);

        // Assert
        result.Should().BeTrue();
        
        // Give time for cancellation to be processed
        await Task.Delay(200);
        
        // Use a fresh context to check the final state
        using var checkContext = new StorageLabelsDbContext(options);
        var finalRotation = await checkContext.EncryptionKeyRotations.FindAsync(rotation.Id);
        finalRotation!.Status.Should().Be(RotationStatus.Cancelled);
        
        // Clean up
        delayedServiceProvider.Dispose();
        testContext.Dispose();
        testConnection.Close();
        testConnection.Dispose();
    }

    [Fact]
    public async Task CancelRotationAsync_ReturnsFalseForCompletedRotation()
    {
        // Arrange
        var key = await _encryptionService.CreateKeyAsync("Test Key", "user123");
        await _encryptionService.ActivateKeyAsync(key.Kid);
        
        var rotation = new EncryptionKeyRotation
        {
            FromKeyId = null,
            ToKeyId = key.Kid,
            Status = RotationStatus.Completed,
            TotalImages = 0,
            InitiatedBy = "user123",
            CompletedAt = DateTime.UtcNow
        };
        
        _context.EncryptionKeyRotations.Add(rotation);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.CancelRotationAsync(rotation.Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetRotationsAsync_ReturnsAllRotations()
    {
        // Arrange
        var key = await _encryptionService.CreateKeyAsync("Test Key", "user123");
        await _encryptionService.ActivateKeyAsync(key.Kid);
        
        var options1 = new RotationOptions(null, key.Kid, 100, "user123", false);
        var options2 = new RotationOptions(null, key.Kid, 100, "user123", false);
        
        await _service.StartRotationAsync(options1);
        await _service.StartRotationAsync(options2);

        // Act
        var rotations = await _service.GetRotationsAsync();

        // Assert
        rotations.Should().HaveCount(2);
        rotations.Should().AllSatisfy(r => r.ToKeyId.Should().Be(key.Kid));
    }

    [Fact]
    public async Task GetRotationsAsync_FiltersByStatus()
    {
        // Arrange
        var key = await _encryptionService.CreateKeyAsync("Test Key", "user123");
        await _encryptionService.ActivateKeyAsync(key.Kid);
        
        var rotation1 = new EncryptionKeyRotation
        {
            FromKeyId = null,
            ToKeyId = key.Kid,
            Status = RotationStatus.InProgress,
            TotalImages = 0,
            InitiatedBy = "user123"
        };
        
        var rotation2 = new EncryptionKeyRotation
        {
            FromKeyId = null,
            ToKeyId = key.Kid,
            Status = RotationStatus.Completed,
            TotalImages = 0,
            InitiatedBy = "user123",
            CompletedAt = DateTime.UtcNow
        };
        
        _context.EncryptionKeyRotations.AddRange(rotation1, rotation2);
        await _context.SaveChangesAsync();

        // Act
        var inProgressRotations = await _service.GetRotationsAsync(RotationStatus.InProgress);

        // Assert
        inProgressRotations.Should().ContainSingle();
        inProgressRotations.First().Status.Should().Be(RotationStatus.InProgress);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}
