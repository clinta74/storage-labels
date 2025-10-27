using MediatR;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record RemoveUserFromLocation(string UserId, long LocationId, string TargetUserId) : IRequest<Result>;

public class RemoveUserFromLocationHandler(StorageLabelsDbContext dbContext) : IRequestHandler<RemoveUserFromLocation, Result>
{
    public async Task<Result> Handle(RemoveUserFromLocation request, CancellationToken cancellationToken)
    {
        // Verify the requesting user has owner access to this location
        var hasOwnerAccess = await dbContext.UserLocations
            .AsNoTracking()
            .AnyAsync(ul => ul.LocationId == request.LocationId 
                && ul.UserId == request.UserId 
                && ul.AccessLevel == AccessLevels.Owner, 
                cancellationToken);

        if (!hasOwnerAccess)
        {
            return Result.Forbidden();
        }

        // Get the user location to check access level
        var userLocation = await dbContext.UserLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.LocationId == request.LocationId 
                && ul.UserId == request.TargetUserId, 
                cancellationToken);

        if (userLocation == null)
        {
            return Result.NotFound("User access not found");
        }

        // Prevent removing the last owner
        if (userLocation.AccessLevel == AccessLevels.Owner)
        {
            var ownerCount = await dbContext.UserLocations
                .CountAsync(ul => ul.LocationId == request.LocationId 
                    && ul.AccessLevel == AccessLevels.Owner, 
                    cancellationToken);

            if (ownerCount <= 1)
            {
                return Result.Error("Cannot remove user. Location must have at least one owner.");
            }
        }

        await dbContext.UserLocations
            .Where(ul => ul.LocationId == request.LocationId && ul.UserId == request.TargetUserId)
            .ExecuteDeleteAsync(cancellationToken);

        return Result.Success();
    }
}
