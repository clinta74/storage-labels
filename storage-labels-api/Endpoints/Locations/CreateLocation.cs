using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Location;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints
{
    private static async Task<Created<LocationResponse>> CreateLocation(HttpContext context, LocationRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var dateTime = timeProvider.GetUtcNow();

        using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var location = dbContext.Locations.Add(new(
            LocationId: 0,
            Name: request.Name,
            Created: dateTime,
            Updated: dateTime
        ));
        await dbContext.SaveChangesAsync(cancellationToken);

        var locationId = location.Entity.LocationId;
        var defaultAccessLevel = AccessLevels.Owner;

        dbContext.UserLocations.Add(new(
            UserId: userId,
            LocationId: locationId,
            AccessLevel: defaultAccessLevel,
            Created: dateTime,
            Updated: dateTime
        ));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return TypedResults.Created((string?)null, new LocationResponse(location.Entity, defaultAccessLevel));
    }
}
