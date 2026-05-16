using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record GetLocation(string UserId, long LocationId) : IRequest<Result<LocationWithAccess>>;
public class GetLocationHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetLocation, Result<LocationWithAccess>>
{
    public async ValueTask<Result<LocationWithAccess>> Handle(GetLocation request, CancellationToken cancellationToken)
    {
        var location = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.LocationId == request.LocationId)
            .Where(ul => ul.UserId == request.UserId)
            .Where(ul => ul.AccessLevel > AccessLevels.None)
            .Select(ul => new LocationWithAccess(ul.Location, ul.AccessLevel))
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            return Result.NotFound($"Location with id {request.LocationId} was not found.");
        }

        return Result.Success(location);
    }
}
