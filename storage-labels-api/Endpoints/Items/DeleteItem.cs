using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Endpoints.Items;

internal partial class ItemEndpoints
{
    private static async Task<Results<Ok, NotFound>> DeleteItem(HttpContext context, [FromRoute] Guid itemId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var hasAccess = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.ItemId == itemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel >= AccessLevels.Edit))
            .AnyAsync(cancellationToken);

        if (!hasAccess)
            return TypedResults.NotFound();

        await dbContext.Items
            .Where(i => i.ItemId == itemId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
