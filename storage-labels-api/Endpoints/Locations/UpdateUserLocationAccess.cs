using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints
{
    private static async Task<Results<Ok<UserLocationResponse>, NotFound<string>, ValidationProblem, ProblemHttpResult>> UpdateUserLocationAccess(HttpContext context, [FromRoute] long locationId, [FromRoute] string userId, UpdateUserLocationRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var requestUserId = context.GetUserId();

        var hasOwnerAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == locationId && ul.UserId == requestUserId && ul.AccessLevel == AccessLevels.Owner, cancellationToken);

        if (!hasOwnerAccess)
            return TypedResults.Problem(statusCode: 403);

        if (request.AccessLevel == AccessLevels.Owner)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.AccessLevel)] = ["Cannot grant Owner access level. Only the location creator has Owner access."] });

        var userLocation = await dbContext.UserLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.LocationId == locationId && ul.UserId == userId, cancellationToken);

        if (userLocation is null)
            return TypedResults.NotFound("User access not found");

        if (userLocation.AccessLevel == AccessLevels.Owner)
            return TypedResults.Problem("Cannot change the access level of the location owner.", statusCode: 500);

        var now = timeProvider.GetUtcNow();

        await dbContext.UserLocations
            .Where(ul => ul.LocationId == locationId && ul.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ul => ul.AccessLevel, request.AccessLevel)
                .SetProperty(ul => ul.Updated, now),
                cancellationToken);

        var updated = await dbContext.UserLocations
            .Include(ul => ul.User)
            .FirstAsync(ul => ul.UserId == userId && ul.LocationId == locationId, cancellationToken);

        return TypedResults.Ok(new UserLocationResponse(updated));
    }
}
