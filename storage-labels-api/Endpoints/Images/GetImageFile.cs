using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.Images;

internal partial class ImageEndpoints
{
    private static async Task<Results<FileStreamHttpResult, PhysicalFileHttpResult, NotFound<string>, ProblemHttpResult>> GetImageFile([FromRoute] Guid imageId, HttpContext context, [FromServices] StorageLabelsDbContext dbContext, [FromServices] IImageEncryptionService encryptionService, [FromServices] ILogger<ImageEndpoints> logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var image = await dbContext.Images
            .Include(img => img.ReferencedByBoxes)
                .ThenInclude(box => box.Location)
            .Include(img => img.ReferencedByItems)
                .ThenInclude(item => item.Box)
                    .ThenInclude(box => box.Location)
            .FirstOrDefaultAsync(img => img.ImageId == imageId, cancellationToken);

        if (image is null)
        {
            logger.LogImageNotFound(imageId, userId);
            return TypedResults.NotFound("Image not found");
        }

        bool hasAccess;

        if (image.UserId == userId)
        {
            hasAccess = true;
        }
        else
        {
            var userLocationIds = await dbContext.UserLocations
                .Where(ul => ul.UserId == userId && ul.AccessLevel > AccessLevels.None)
                .Select(ul => ul.LocationId)
                .ToListAsync(cancellationToken);

            hasAccess = image.ReferencedByBoxes.Any(box => userLocationIds.Contains(box.LocationId))
                     || image.ReferencedByItems.Any(item => userLocationIds.Contains(item.Box.LocationId));
        }

        if (!hasAccess)
        {
            logger.LogImageForbiddenAccess(imageId, userId);
            return TypedResults.Problem("Access denied", statusCode: 403);
        }

        if (!System.IO.File.Exists(image.StoragePath))
        {
            logger.LogImageFileNotFound(imageId, userId);
            return TypedResults.NotFound("Image file not found");
        }

        if (!image.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogImageInvalidContentType(imageId, userId);
            return TypedResults.Problem("Invalid content type", statusCode: 500);
        }

        if (image.IsEncrypted)
        {
            try
            {
                var decryptedStream = await encryptionService.DecryptImageAsync(image, cancellationToken);
                logger.LogImageServedToOwner(imageId, userId);
                return TypedResults.Stream(decryptedStream, image.ContentType, image.FileName);
            }
            catch (Exception ex)
            {
                logger.ImageDecryptionFailed(ex, imageId, userId);
                return TypedResults.Problem("Failed to decrypt image", statusCode: 500);
            }
        }

        logger.LogImageServedToOwner(imageId, userId);
        return TypedResults.PhysicalFile(image.StoragePath, image.ContentType, image.FileName);
    }
}
