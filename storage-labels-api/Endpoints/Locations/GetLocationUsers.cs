using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.User;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints
{
    private static async Task<Results<Ok<IEnumerable<UserLocationResponse>>, ProblemHttpResult>> GetLocationUsers(HttpContext context, [FromRoute] long locationId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var hasAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == locationId && ul.UserId == userId && ul.AccessLevel >= AccessLevels.Edit, cancellationToken);

        if (!hasAccess)
            return TypedResults.Problem(statusCode: 403);

        var userLocations = await dbContext.UserLocations
            .AsNoTracking()
            .Include(ul => ul.User)
            .Where(ul => ul.LocationId == locationId)
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IEnumerable<UserLocationResponse>>(userLocations.Select(u => new UserLocationResponse(u)));
    }
}
