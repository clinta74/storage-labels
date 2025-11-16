using Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Images;

public record UploadImage(IFormFile File, string UserId) : IRequest<Result<ImageMetadata>>;

public class UploadImageHandler : IRequestHandler<UploadImage, Result<ImageMetadata>>
{
    private readonly StorageLabelsDbContext _dbContext;
    private readonly ILogger<UploadImageHandler> _logger;
    private readonly string _storagePath;

    public UploadImageHandler(
        StorageLabelsDbContext dbContext,
        ILogger<UploadImageHandler> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _logger = logger;
        
        // Use configured path or default to /app/data/images for Docker compatibility
        var basePath = configuration["ImageStoragePath"] ?? "/app/data/images";
        _storagePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(_storagePath);
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

        // Save the file
        await using (var fileStream = File.Create(storagePath))
        {
            await request.File.CopyToAsync(fileStream, cancellationToken);
        }

        var metadata = new ImageMetadata
        {
            ImageId = imageId,
            UserId = request.UserId,
            FileName = fileName,
            ContentType = request.File.ContentType,
            StoragePath = storagePath,
            UploadedAt = DateTime.UtcNow,
            SizeInBytes = request.File.Length
        };

        _dbContext.Images.Add(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogImageUploaded(imageId, request.UserId, fileName);

        return Result.Success(metadata);
    }
}
