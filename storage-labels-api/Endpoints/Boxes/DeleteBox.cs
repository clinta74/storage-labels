using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Box;

namespace StorageLabelsApi.Endpoints.Boxes;

internal static partial class BoxEndpoints
{
    private static async Task<Results<Ok, NotFound<string>, ValidationProblem>> DeleteBox(HttpContext context, [FromRoute] Guid boxId, [FromServices] StorageLabelsDbContext dbContext, ILogger logger, CancellationToken cancellationToken, [FromQuery] bool force = false)
    {
        var userId = context.GetUserId();

        var box = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == boxId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel == AccessLevels.Owner))
            .Include(b => b.Items)
            .FirstOrDefaultAsync(cancellationToken);

        if (box is null)
        {
            return TypedResults.NotFound($"Box with id ({boxId})");
        }

        if (box.Items.Count > 0 && !force)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(Box)] = box.Items.Select(item => $"Box id ({box.BoxId}) with name ({box.Name}) with item id ({item.ItemId}).").ToArray()
            });
        }

        if (force && box.Items.Count > 0)
        {
            await dbContext.Items
                .Where(i => i.BoxId == boxId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await dbContext.Boxes
            .Where(b => b.BoxId == boxId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.DeleteBox(boxId);

        return TypedResults.Ok();
    }
}
