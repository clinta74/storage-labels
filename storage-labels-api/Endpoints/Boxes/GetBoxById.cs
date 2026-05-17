using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Box;

namespace StorageLabelsApi.Endpoints.Boxes;

internal partial class BoxEndpoints
{
    private static async Task<Results<Ok<BoxResponse>, NotFound<string>>> GetBoxById(HttpContext context, [FromRoute] Guid boxId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == boxId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == userId))
            .Where(b => b.Location.UserLocations.Where(ul => ul.UserId == userId).First().AccessLevel > AccessLevels.None)
            .FirstOrDefaultAsync(cancellationToken);

        if (box is null)
        {
            return TypedResults.NotFound($"Box with id {boxId} was not found.");
        }

        return TypedResults.Ok(new BoxResponse(box));
    }
}
