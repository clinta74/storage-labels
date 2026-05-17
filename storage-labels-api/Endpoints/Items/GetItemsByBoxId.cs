using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Item;

namespace StorageLabelsApi.Endpoints.Items;

internal partial class ItemEndpoints
{
    private static async IAsyncEnumerable<ItemResponse> GetItemsByBoxId(HttpContext context, [FromRoute] Guid boxId, [FromServices] StorageLabelsDbContext dbContext, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var items = dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel >= AccessLevels.View))
            .Include(b => b.Items)
            .SelectMany(b => b.Items)
            .Where(i => i.BoxId == boxId)
            .AsAsyncEnumerable();

        await foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new ItemResponse(item);
        }
    }
}
