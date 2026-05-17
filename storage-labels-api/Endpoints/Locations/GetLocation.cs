using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Location;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints
{
    private static async Task<Results<Ok<LocationResponse>, NotFound<string>>> GetLocation(HttpContext context, [FromRoute] long locationId, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var location = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.LocationId == locationId)
            .Where(ul => ul.UserId == userId)
            .Where(ul => ul.AccessLevel > AccessLevels.None)
            .Select(ul => new LocationResponse(ul.Location.LocationId, ul.Location.Name, ul.AccessLevel, ul.Location.Created, ul.Location.Updated))
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
            return TypedResults.NotFound($"Location with id {locationId} was not found.");

        return TypedResults.Ok(location);
    }
}
