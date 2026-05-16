using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Locations;

internal static partial class LocationEndpoints
{
    private static async Task<Results<Created<UserLocationResponse>, NotFound<string>, ValidationProblem, ProblemHttpResult>> AddUserToLocation(HttpContext context, [FromRoute] long locationId, AddUserLocationRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var hasOwnerAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == locationId && ul.UserId == userId && ul.AccessLevel == AccessLevels.Owner, cancellationToken);

        if (!hasOwnerAccess)
            return TypedResults.Problem(statusCode: 403);

        if (request.AccessLevel == AccessLevels.Owner)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.AccessLevel)] = ["Cannot grant Owner access level. Only the location creator has Owner access."] });

        var targetUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress, cancellationToken);

        if (targetUser is null)
            return TypedResults.NotFound($"User with email {request.EmailAddress} not found");

        var existingAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == locationId && ul.UserId == targetUser.UserId, cancellationToken);

        if (existingAccess)
            return TypedResults.Problem("User already has access to this location", statusCode: 409);

        var now = timeProvider.GetUtcNow();
        dbContext.UserLocations.Add(new UserLocation(
            targetUser.UserId,
            locationId,
            request.AccessLevel,
            now,
            now
        ));
        await dbContext.SaveChangesAsync(cancellationToken);

        var result = await dbContext.UserLocations
            .Include(ul => ul.User)
            .FirstAsync(ul => ul.UserId == targetUser.UserId && ul.LocationId == locationId, cancellationToken);

        return TypedResults.Created((string?)null, new UserLocationResponse(result));
    }
}
