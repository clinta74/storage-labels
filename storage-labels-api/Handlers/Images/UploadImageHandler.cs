using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.AspNetCore.Http;
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
        ILogger<UploadImageHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
        _storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "StorageLabels", "Images");
        Directory.CreateDirectory(_storagePath);
    }

    public async Task<Result<ImageMetadata>> Handle(UploadImage request, CancellationToken cancellationToken)
    {
        if (request.File.ContentType != "image/jpeg")
        {
            return Result.Error("Only JPEG images are supported");
        }

        var hashedUserId = HashUserId(request.UserId);
        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(request.File.FileName);
        var fileGuid = Guid.CreateVersion7();
        var userDir = Path.Combine(_storagePath, hashedUserId);
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
            HashedUserId = hashedUserId,
            StoragePath = storagePath,
            UploadedAt = DateTime.UtcNow,
            SizeInBytes = request.File.Length
        };

        _dbContext.Images.Add(metadata);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogImageUploaded(imageId, request.UserId, fileName);

        return Result.Success(metadata);
    }

    private static string HashUserId(string userId)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
