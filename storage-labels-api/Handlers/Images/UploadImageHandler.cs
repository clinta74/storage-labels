using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.Images;

public record UploadImage(IFormFile File, string UserId, bool Encrypt = true) : IRequest<Result<ImageMetadata>>;

public class UploadImageHandler : IRequestHandler<UploadImage, Result<ImageMetadata>>
{
    private readonly TimeProvider _timeProvider;
    private readonly StorageLabelsDbContext _dbContext;
    private readonly ILogger<UploadImageHandler> _logger;
    private readonly IImageEncryptionService _encryptionService;
    private readonly string _storagePath;

    public UploadImageHandler(
        StorageLabelsDbContext dbContext,
        ILogger<UploadImageHandler> logger,
        IImageEncryptionService encryptionService,
        IConfiguration configuration,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _encryptionService = encryptionService;
        _timeProvider = timeProvider;
        _storagePath = configuration["IMAGE_STORAGE_PATH"] ?? "/app/data/images";
    }

    public async ValueTask<Result<ImageMetadata>> Handle(UploadImage request, CancellationToken cancellationToken)
    {
        const long maxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

        if (request.File.ContentType != "image/jpeg")
        {
            return Result.Error("Only JPEG images are supported");
        }

        if (request.File.Length > maxFileSizeInBytes)
        {
            return Result.Error($"File size exceeds the maximum allowed size of {maxFileSizeInBytes / 1024 / 1024} MB");
        }

        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(request.File.FileName);
        var fileGuid = Guid.CreateVersion7();
        
        // Sanitize userId for use in file path (replace invalid characters)
        var sanitizedUserId = string.Join("_", request.UserId.Split(Path.GetInvalidFileNameChars()));
        var userDir = Path.Combine(_storagePath, sanitizedUserId);
        Directory.CreateDirectory(userDir);
        var storagePath = Path.Combine(userDir, $"{fileGuid}{Path.GetExtension(fileName)}");

        long fileSizeBytes;
        int? encryptionKeyId = null;
        byte[]? iv = null;
        byte[]? authTag = null;

        // Encrypt and save the file
        if (request.Encrypt)
        {
            try
            {
                // Check if an active encryption key exists
                var activeKey = await _encryptionService.GetActiveKeyAsync(cancellationToken);
                if (activeKey == null)
                {
                    _logger.LogWarning("No active encryption key found. Saving image unencrypted.");
                    // Fall back to unencrypted storage
                    await using var fileStream = File.Create(storagePath);
                    await request.File.CopyToAsync(fileStream, cancellationToken);
                    fileSizeBytes = request.File.Length;
                }
                else
                {
                    // Encrypt the image
                    using var inputStream = request.File.OpenReadStream();
                    var encryptionResult = await _encryptionService.EncryptAsync(inputStream, cancellationToken);

                    // Save encrypted data to disk
                    await File.WriteAllBytesAsync(storagePath, encryptionResult.EncryptedData, cancellationToken);
                    
                    fileSizeBytes = encryptionResult.EncryptedData.Length;
                    encryptionKeyId = encryptionResult.EncryptionKeyId;
                    iv = encryptionResult.InitializationVector;
                    authTag = encryptionResult.AuthenticationTag;

                    _logger.LogInformation(
                        "Image {ImageId} encrypted with key {Kid}",
                        imageId,
                        activeKey.Kid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt image {ImageId}. Saving unencrypted.", imageId);
                // Fall back to unencrypted storage
                await using var fileStream = File.Create(storagePath);
                await request.File.CopyToAsync(fileStream, cancellationToken);
                fileSizeBytes = request.File.Length;
            }
        }
        else
        {
            // Save unencrypted
            await using var fileStream = File.Create(storagePath);
            await request.File.CopyToAsync(fileStream, cancellationToken);
            fileSizeBytes = request.File.Length;
        }

        var metadata = new ImageMetadata
        {
            ImageId = imageId,
            UserId = request.UserId,
            FileName = fileName,
            ContentType = request.File.ContentType,
            StoragePath = storagePath,
            UploadedAt = _timeProvider.GetUtcNow().DateTime,
            SizeInBytes = fileSizeBytes,
            IsEncrypted = encryptionKeyId.HasValue,
            EncryptionKeyId = encryptionKeyId,
            InitializationVector = iv,
            AuthenticationTag = authTag
        };

        _dbContext.Images.Add(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogImageUploaded(imageId, request.UserId, fileName);

        return Result.Success(metadata);
    }
}

