using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record UpdateLocation(string UserId, long LocationId, string Name) : IRequest<Result<LocationWithAccess>>;

public class UpdateLocationHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<UpdateLocation, Result<LocationWithAccess>>
{
    public async Task<Result<LocationWithAccess>> Handle(UpdateLocation request, CancellationToken cancellationToken)
    {
        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == request.LocationId)
            .Where(l => l.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel > AccessLevels.View))
            .Select(l => new LocationWithAccess(l, l.UserLocations.First(ul => ul.UserId == request.UserId).AccessLevel))
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            return Result.NotFound($"Location with id {request.LocationId} was not found.");
        }

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Locations
            .Where(l => l.LocationId == request.LocationId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(l => l.Name, request.Name)
                .SetProperty(l => l.Updated, dateTime),
                cancellationToken);

        // Reload the updated location
        var updatedLocation = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == request.LocationId)
            .FirstAsync(cancellationToken);

        return Result.Success(new LocationWithAccess(updatedLocation, location.AccessLevel));
    }
}
