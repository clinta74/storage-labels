using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Boxes;

public record MoveBox(Guid BoxId, long DestinationLocationId, string UserId) : IRequest<Result<Box>>;

public class MoveBoxHandler(
    StorageLabelsDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<MoveBoxHandler> logger) : IRequestHandler<MoveBox, Result<Box>>
{
    public async Task<Result<Box>> Handle(MoveBox request, CancellationToken cancellationToken)
    {
        // Check if the box exists and user has edit access to the source location
        var userCanAccessSourceLocation = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == request.BoxId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == request.UserId && 
                (ul.AccessLevel == AccessLevels.Edit || ul.AccessLevel == AccessLevels.Owner)))
            .AnyAsync(cancellationToken);

        if (!userCanAccessSourceLocation)
        {
            return Result<Box>.NotFound($"Box with id ({request.BoxId}) not found or you don't have permission to move it.");
        }

        // Check if destination location exists and user has edit access
        var userCanAccessDestinationLocation = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == request.DestinationLocationId)
            .Where(l => l.UserLocations.Any(ul => ul.UserId == request.UserId && 
                (ul.AccessLevel == AccessLevels.Edit || ul.AccessLevel == AccessLevels.Owner)))
            .AnyAsync(cancellationToken);

        if (!userCanAccessDestinationLocation)
        {
            logger.NoAccessToLocation(request.UserId, request.DestinationLocationId);
            return Result<Box>.Invalid(new ValidationError
            {
                Identifier = nameof(MoveBox.DestinationLocationId),
                ErrorCode = "DestinationLocationNotFound",
                ErrorMessage = $"Destination location with id ({request.DestinationLocationId}) not found or you don't have edit permission for it.",
                Severity = ValidationSeverity.Error,
            });
        }

        var dateTime = timeProvider.GetUtcNow();

        // Move the box using ExecuteUpdateAsync
        await dbContext.Boxes
            .Where(b => b.BoxId == request.BoxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.LocationId, request.DestinationLocationId)
                .SetProperty(b => b.Updated, dateTime),
                cancellationToken);

        // Reload the updated box
        var updatedBox = await dbContext.Boxes
            .AsNoTracking()
            .FirstAsync(b => b.BoxId == request.BoxId, cancellationToken);

        return Result<Box>.Success(updatedBox);
    }
}
