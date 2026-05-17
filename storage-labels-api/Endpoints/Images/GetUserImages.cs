using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO.Image;

namespace StorageLabelsApi.Endpoints.Images;

internal partial class ImageEndpoints
{
    private static async Task<Ok<List<ImageMetadataResponse>>> GetUserImages(HttpContext context, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var images = await dbContext.Images
            .AsNoTracking()
            .Include(img => img.ReferencedByBoxes)
            .Include(img => img.ReferencedByItems)
            .Where(img => img.UserId == userId)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(images.ConvertAll(img => new ImageMetadataResponse(img, string.Empty)));
    }
}
