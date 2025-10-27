using MediatR;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record UpdateUserLocationAccess(string UserId, long LocationId, string TargetUserId, AccessLevels AccessLevel) : IRequest<Result<UserLocation>>;

public class UpdateUserLocationAccessHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<UpdateUserLocationAccess, Result<UserLocation>>
{
    public async Task<Result<UserLocation>> Handle(UpdateUserLocationAccess request, CancellationToken cancellationToken)
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

        // Prevent setting Owner access level
        if (request.AccessLevel == AccessLevels.Owner)
        {
            return Result.Invalid(new ValidationError(nameof(request.AccessLevel), "Cannot grant Owner access level. Only the location creator has Owner access.", "AccessLevel", ValidationSeverity.Error));
        }

        // Get the user location to update
        var userLocation = await dbContext.UserLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.LocationId == request.LocationId 
                && ul.UserId == request.TargetUserId, 
                cancellationToken);

        if (userLocation == null)
        {
            return Result.NotFound("User access not found");
        }

        // Prevent changing owner's access level
        if (userLocation.AccessLevel == AccessLevels.Owner)
        {
            return Result.Error("Cannot change the access level of the location owner.");
        }

        var now = timeProvider.GetUtcNow();
        var updatedUserLocation = userLocation with
        {
            AccessLevel = request.AccessLevel,
            Updated = now
        };

        dbContext.UserLocations.Update(updatedUserLocation);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload with user data
        var result = await dbContext.UserLocations
            .Include(ul => ul.User)
            .FirstAsync(ul => ul.UserId == request.TargetUserId 
                && ul.LocationId == request.LocationId, 
                cancellationToken);

        return Result.Success(result);
    }
}
