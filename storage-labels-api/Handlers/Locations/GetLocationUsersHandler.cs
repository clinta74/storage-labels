using MediatR;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record GetLocationUsers(string UserId, long LocationId) : IRequest<Result<List<UserLocation>>>;

public class GetLocationUsersHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetLocationUsers, Result<List<UserLocation>>>
{
    public async Task<Result<List<UserLocation>>> Handle(GetLocationUsers request, CancellationToken cancellationToken)
    {
        // Verify the user has access to this location
        var hasAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == request.LocationId 
                && ul.UserId == request.UserId 
                && ul.AccessLevel >= AccessLevels.Edit, 
                cancellationToken);

        if (!hasAccess)
        {
            return Result.Forbidden();
        }

        var userLocations = await dbContext.UserLocations
            .AsNoTracking()
            .Include(ul => ul.User)
            .Where(ul => ul.LocationId == request.LocationId)
            .ToListAsync(cancellationToken);

        return Result.Success(userLocations);
    }
}
