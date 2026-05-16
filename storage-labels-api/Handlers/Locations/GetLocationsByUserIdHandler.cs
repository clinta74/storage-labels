using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record GetLocationsByUserId(string UserId) : IRequest<Result<List<LocationWithAccess>>>;
public class GetLocationsByUserIdHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetLocationsByUserId, Result<List<LocationWithAccess>>>
{
    public async ValueTask<Result<List<LocationWithAccess>>> Handle(GetLocationsByUserId request, CancellationToken cancellationToken)
    {
        var locations = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == request.UserId)
            .Where(ul => ul.AccessLevel > AccessLevels.None)
            .Select(ul => new LocationWithAccess(ul.Location, ul.AccessLevel))
            .ToListAsync(cancellationToken);

        return Result.Success(locations);
    }
}
