using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.Images;

internal static partial class ImageEndpoints
{
    private static async Task<Results<Ok<ImageMetadata>, ProblemHttpResult>> UploadImage(IFormFile file, HttpContext context, [FromServices] StorageLabelsDbContext dbContext, [FromServices] IImageEncryptionService encryptionService, [FromServices] IConfiguration configuration, [FromServices] TimeProvider timeProvider, ILogger logger, CancellationToken cancellationToken)
    {
        const long maxFileSizeInBytes = 10 * 1024 * 1024; // 10 MB

        if (file.ContentType != "image/jpeg")
            return TypedResults.Problem("Only JPEG images are supported", statusCode: 400);

        if (file.Length > maxFileSizeInBytes)
            return TypedResults.Problem($"File size exceeds the maximum allowed size of {maxFileSizeInBytes / 1024 / 1024} MB", statusCode: 400);

        var userId = context.GetUserId();
        var storagePath = configuration["IMAGE_STORAGE_PATH"] ?? "/app/data/images";
        var imageId = Guid.NewGuid();
        var fileName = Path.GetFileName(file.FileName);
        var fileGuid = Guid.CreateVersion7();

        var sanitizedUserId = string.Join("_", userId.Split(Path.GetInvalidFileNameChars()));
        var userDir = Path.Combine(storagePath, sanitizedUserId);
        Directory.CreateDirectory(userDir);
        var filePath = Path.Combine(userDir, $"{fileGuid}{Path.GetExtension(fileName)}");

        long fileSizeBytes;
        int? encryptionKeyId = null;
        byte[]? iv = null;
        byte[]? authTag = null;

        try
        {
            var activeKey = await encryptionService.GetActiveKeyAsync(cancellationToken);
            if (activeKey is null)
            {
                logger.NoActiveEncryptionKeyFound();
                await using var fs = File.Create(filePath);
                await file.CopyToAsync(fs, cancellationToken);
                fileSizeBytes = file.Length;
            }
            else
            {
                using var inputStream = file.OpenReadStream();
                var encryptionResult = await encryptionService.EncryptAsync(inputStream, cancellationToken);
                await File.WriteAllBytesAsync(filePath, encryptionResult.EncryptedData, cancellationToken);
                fileSizeBytes = encryptionResult.EncryptedData.Length;
                encryptionKeyId = encryptionResult.EncryptionKeyId;
                iv = encryptionResult.InitializationVector;
                authTag = encryptionResult.AuthenticationTag;
                logger.ImageEncryptedWithKey(imageId, activeKey.Kid);
            }
        }
        catch (Exception ex)
        {
            logger.ImageEncryptionFailed(ex, imageId);
            await using var fs = File.Create(filePath);
            await file.CopyToAsync(fs, cancellationToken);
            fileSizeBytes = file.Length;
        }

        var metadata = new ImageMetadata
        {
            ImageId = imageId,
            UserId = userId,
            FileName = fileName,
            ContentType = file.ContentType,
            StoragePath = filePath,
            UploadedAt = timeProvider.GetUtcNow().DateTime,
            SizeInBytes = fileSizeBytes,
            IsEncrypted = encryptionKeyId.HasValue,
            EncryptionKeyId = encryptionKeyId,
            InitializationVector = iv,
            AuthenticationTag = authTag
        };

        dbContext.Images.Add(metadata);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogImageUploaded(imageId, userId, fileName);

        return TypedResults.Ok(metadata);
    }
}
