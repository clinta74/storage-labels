using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Box;

namespace StorageLabelsApi.Endpoints.Boxes;

internal partial class BoxEndpoints
{
    private static async IAsyncEnumerable<BoxResponse> GetBoxesByLocationId(HttpContext context, [FromRoute] long locationId, [FromServices] StorageLabelsDbContext dbContext, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var boxes = dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.LocationId == locationId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel >= AccessLevels.View))
            .AsAsyncEnumerable();

        await foreach (var box in boxes)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new BoxResponse(box);
        }
    }
}
