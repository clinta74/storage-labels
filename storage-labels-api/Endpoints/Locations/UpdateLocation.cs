using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Location;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints
{
    private static async Task<Results<Ok<LocationResponse>, NotFound<string>>> UpdateLocation(HttpContext context, [FromRoute] long locationId, LocationRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var accessLevel = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.LocationId == locationId && ul.UserId == userId && ul.AccessLevel > AccessLevels.View)
            .Select(ul => (AccessLevels?)ul.AccessLevel)
            .FirstOrDefaultAsync(cancellationToken);

        if (accessLevel is null)
            return TypedResults.NotFound($"Location with id {locationId} was not found.");

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Locations
            .Where(l => l.LocationId == locationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(l => l.Name, request.Name)
                .SetProperty(l => l.Updated, dateTime),
                cancellationToken);

        var updatedLocation = await dbContext.Locations
            .AsNoTracking()
            .FirstAsync(l => l.LocationId == locationId, cancellationToken);

        return TypedResults.Ok(new LocationResponse(updatedLocation, accessLevel.Value));
    }
}
