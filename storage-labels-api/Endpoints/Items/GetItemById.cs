using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Item;

namespace StorageLabelsApi.Endpoints.Items;

internal partial class ItemEndpoints
{
    private static async Task<Results<Ok<ItemResponse>, NotFound>> GetItemById(HttpContext context, [FromRoute] Guid itemId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.ItemId == itemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel >= AccessLevels.View))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return TypedResults.NotFound();

        return TypedResults.Ok(new ItemResponse(item));
    }
}
