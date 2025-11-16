using Mediator;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record AddUserToLocation(string UserId, long LocationId, string EmailAddress, AccessLevels AccessLevel) : IRequest<Result<UserLocation>>;

public class AddUserToLocationHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<AddUserToLocation, Result<UserLocation>>
{
    public async ValueTask<Result<UserLocation>> Handle(AddUserToLocation request, CancellationToken cancellationToken)
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

        // Find the user by email
        var targetUser = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailAddress == request.EmailAddress, cancellationToken);

        if (targetUser == null)
        {
            return Result.NotFound($"User with email {request.EmailAddress} not found");
        }

        // Check if user already has access
        var existingAccess = await dbContext.UserLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(ul => ul.LocationId == request.LocationId 
                && ul.UserId == targetUser.UserId, 
                cancellationToken);

        if (existingAccess != null)
        {
            return Result.Conflict("User already has access to this location");
        }

        var now = timeProvider.GetUtcNow();
        var userLocation = new UserLocation(
            targetUser.UserId,
            request.LocationId,
            request.AccessLevel,
            now,
            now
        );

        dbContext.UserLocations.Add(userLocation);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Reload with user data
        var result = await dbContext.UserLocations
            .Include(ul => ul.User)
            .FirstAsync(ul => ul.UserId == targetUser.UserId 
                && ul.LocationId == request.LocationId, 
                cancellationToken);

        return Result.Success(result);
    }
}
