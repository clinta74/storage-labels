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
    private static async Task<Results<Ok<BoxResponse>, NotFound<string>, ValidationProblem>> MoveBox(HttpContext context, [FromRoute] Guid boxId, [FromBody] MoveBoxRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var userCanAccessSourceLocation = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == boxId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == userId &&
                (ul.AccessLevel == AccessLevels.Edit || ul.AccessLevel == AccessLevels.Owner)))
            .AnyAsync(cancellationToken);

        if (!userCanAccessSourceLocation)
        {
            return TypedResults.NotFound($"Box with id ({boxId}) not found or you don't have permission to move it.");
        }

        var userCanAccessDestinationLocation = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == request.DestinationLocationId)
            .Where(l => l.UserLocations.Any(ul => ul.UserId == userId &&
                (ul.AccessLevel == AccessLevels.Edit || ul.AccessLevel == AccessLevels.Owner)))
            .AnyAsync(cancellationToken);

        if (!userCanAccessDestinationLocation)
        {
            logger.NoAccessToLocation(userId, request.DestinationLocationId);
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(MoveBoxRequest.DestinationLocationId)] = [$"Destination location with id ({request.DestinationLocationId}) not found or you don't have edit permission for it."]
            });
        }

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Boxes
            .Where(b => b.BoxId == boxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.LocationId, request.DestinationLocationId)
                .SetProperty(b => b.Updated, dateTime),
                cancellationToken);

        var updatedBox = await dbContext.Boxes
            .AsNoTracking()
            .FirstAsync(b => b.BoxId == boxId, cancellationToken);

        return TypedResults.Ok(new BoxResponse(updatedBox));
    }
}
