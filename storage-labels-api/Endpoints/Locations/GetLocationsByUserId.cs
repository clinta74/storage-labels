using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Location;

namespace StorageLabelsApi.Endpoints.Locations;

internal static partial class LocationEndpoints
{
    private static async Task<Ok<List<LocationResponse>>> GetLocationsByUserId(HttpContext context, [FromServices] StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var locations = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == userId)
            .Where(ul => ul.AccessLevel > AccessLevels.None)
            .Select(ul => new LocationResponse(ul.Location.LocationId, ul.Location.Name, ul.AccessLevel, ul.Location.Created, ul.Location.Updated))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(locations);
    }
}
