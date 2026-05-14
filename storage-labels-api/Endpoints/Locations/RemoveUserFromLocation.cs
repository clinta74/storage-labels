using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Endpoints.Locations;

internal static partial class LocationEndpoints
{
    private static async Task<Results<Ok, NotFound<string>, ProblemHttpResult>> RemoveUserFromLocation(HttpContext context, [FromRoute] long locationId, [FromRoute] string userId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var requestUserId = context.GetUserId();

        var hasOwnerAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == locationId && ul.UserId == requestUserId && ul.AccessLevel == AccessLevels.Owner, cancellationToken);

        if (!hasOwnerAccess)
            return TypedResults.Problem(statusCode: 403);

        var userLocation = await dbContext.UserLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.LocationId == locationId && ul.UserId == userId, cancellationToken);

        if (userLocation is null)
            return TypedResults.NotFound("User access not found");

        if (userLocation.AccessLevel == AccessLevels.Owner)
        {
            var ownerCount = await dbContext.UserLocations
                .CountAsync(ul => ul.LocationId == locationId && ul.AccessLevel == AccessLevels.Owner, cancellationToken);

            if (ownerCount <= 1)
                return TypedResults.Problem("Cannot remove user. Location must have at least one owner.", statusCode: 500);
        }

        await dbContext.UserLocations
            .Where(ul => ul.LocationId == locationId && ul.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.Ok();
    }
}
