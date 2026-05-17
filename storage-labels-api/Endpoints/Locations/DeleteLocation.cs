using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints
{
    private static async Task<Results<Ok, NotFound<string>, ValidationProblem>> DeleteLocation(HttpContext context, [FromRoute] long locationId, [FromServices] StorageLabelsDbContext dbContext, [FromServices] ILogger<LocationEndpoints> logger, CancellationToken cancellationToken, [FromQuery] bool force = false)
    {
        var userId = context.GetUserId();

        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == locationId)
            .Where(l => l.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel == AccessLevels.Owner))
            .Include(l => l.Boxes)
                .ThenInclude(b => b.Items)
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
            return TypedResults.NotFound($"Location with id {locationId} was not found.");

        if (location.Boxes.Count > 0 && !force)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(Location)] = location.Boxes.Select(box => $"Location id ({location.LocationId}) with name ({location.Name}) has box id ({box.BoxId}).").ToArray()
            });
        }

        if (force && location.Boxes.Count > 0)
        {
            var boxIds = location.Boxes.Select(b => b.BoxId).ToList();
            await dbContext.Items
                .Where(i => boxIds.Contains(i.BoxId))
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.Boxes
                .Where(b => b.LocationId == locationId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await dbContext.Locations
            .Where(l => l.LocationId == locationId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.DeleteLocation(locationId);

        return TypedResults.Ok();
    }
}
