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

        // Every box's item count in one grouped query, ahead of the stream. Counting inside
        // the loop below would be a query per box, which is the round trip this field exists
        // to remove; a box with nothing in it simply has no row here and counts zero.
        var itemCounts = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.Box.LocationId == locationId)
            .GroupBy(i => i.BoxId)
            .Select(group => new { BoxId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.BoxId, row => row.Count, cancellationToken);

        var boxes = dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.LocationId == locationId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel >= AccessLevels.View))
            .AsAsyncEnumerable();

        await foreach (var box in boxes)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new BoxResponse(box, itemCounts.GetValueOrDefault(box.BoxId));
        }
    }
}
