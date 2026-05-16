using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Endpoints.Images;

internal static partial class ImageEndpoints
{
    private static async Task<Results<Ok, NotFound<string>, ProblemHttpResult>> DeleteImage([FromRoute] Guid imageId, HttpContext context, [FromServices] StorageLabelsDbContext dbContext, [FromServices] ILogger logger, CancellationToken cancellationToken)
    {
        return await DeleteImageCore(imageId, context.GetUserId(), false, dbContext, logger, cancellationToken);
    }

    private static async Task<Results<Ok, NotFound<string>, ProblemHttpResult>> ForceDeleteImage([FromRoute] Guid imageId, HttpContext context, [FromServices] StorageLabelsDbContext dbContext, [FromServices] ILogger logger, CancellationToken cancellationToken)
    {
        return await DeleteImageCore(imageId, context.GetUserId(), true, dbContext, logger, cancellationToken);
    }

    private static async Task<Results<Ok, NotFound<string>, ProblemHttpResult>> DeleteImageCore(Guid imageId, string userId, bool force, StorageLabelsDbContext dbContext, ILogger logger, CancellationToken cancellationToken)
    {
        var image = await dbContext.Images
            .AsNoTracking()
            .Include(i => i.ReferencedByBoxes)
            .Include(i => i.ReferencedByItems)
            .FirstOrDefaultAsync(i => i.ImageId == imageId, cancellationToken);

        if (image is null)
            return TypedResults.NotFound("Image not found");

        if (image.UserId != userId)
            return TypedResults.Problem("Not authorized to delete this image", statusCode: 401);

        var hasReferences = image.ReferencedByBoxes.Count != 0 || image.ReferencedByItems.Count != 0;

        if (hasReferences && !force)
        {
            var boxCount = image.ReferencedByBoxes.Count;
            var itemCount = image.ReferencedByItems.Count;
            return TypedResults.Problem($"Cannot delete image that is still referenced by {boxCount} box(es) and {itemCount} item(s). Use force delete to remove references.", statusCode: 500);
        }

        if (force && hasReferences)
        {
            await dbContext.Boxes
                .Where(b => b.ImageMetadataId == imageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(b => b.ImageUrl, (string?)null)
                    .SetProperty(b => b.ImageMetadataId, (Guid?)null),
                    cancellationToken);

            await dbContext.Items
                .Where(i => i.ImageMetadataId == imageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.ImageUrl, (string?)null)
                    .SetProperty(i => i.ImageMetadataId, (Guid?)null),
                    cancellationToken);
        }

        try
        {
            if (File.Exists(image.StoragePath))
                File.Delete(image.StoragePath);
        }
        catch (Exception ex)
        {
            logger.LogImageDeleteError(ex, image.ImageId);
        }

        await dbContext.Images
            .Where(i => i.ImageId == imageId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.LogImageDeleted(image.ImageId, userId);

        return TypedResults.Ok();
    }
}
