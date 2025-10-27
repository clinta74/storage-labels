using Ardalis.Result.FluentValidation;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Boxes;

public record UpdateBox(
    Guid BoxId,
    string Code,
    string Name, 
    string UserId,
    long LocationId,
    string? Description = null,
    string? ImageUrl = null,
    Guid? ImageMetadataId = null) : IRequest<Result<Box>>;


public class UpdateBoxHandler(
    StorageLabelsDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<UpdateBoxHandler> logger) : IRequestHandler<UpdateBox, Result<Box>>
{
    public async Task<Result<Box>> Handle(UpdateBox request, CancellationToken cancellationToken)
    {
        var validation = await new UpdateBoxValidator()
            .ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Result<Box>.Invalid(validation.AsErrors());
        }

        var hasBoxCode = await dbContext.Boxes
            .AsNoTracking()
            .Where(box => box.Code == request.Code)
            .Where(box => box.BoxId != request.BoxId)
            .AnyAsync(cancellationToken);

        if (hasBoxCode)
        {
            return Result.Conflict([$"Box with the code {request.Code} already exists"]);
        }

        var userCanAccessLocation = await dbContext.UserLocations
            .AsNoTracking()
            .Where(userLocation => userLocation.LocationId == request.LocationId)
            .Where(userLocation => userLocation.UserId == request.UserId)
            .Where(userLocation => userLocation.AccessLevel >= AccessLevels.Edit)
            .AnyAsync(cancellationToken);

        if (!userCanAccessLocation)
        {
            logger.NoAccessToLocation(request.UserId, request.LocationId);
            return Result.Invalid(new ValidationError(nameof(Location), $"User cannot add box to location ({request.LocationId}).", "Access", ValidationSeverity.Error));
        }

        var box = await  dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == request.BoxId)
            .SingleOrDefaultAsync(cancellationToken);

        if (box is null)
        {
            return Result.NotFound($"Box with id {request.BoxId} not found.");
        }

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Boxes
            .Where(b => b.BoxId == request.BoxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Code, request.Code)
                .SetProperty(b => b.Name, request.Name)
                .SetProperty(b => b.Description, request.Description)
                .SetProperty(b => b.ImageUrl, request.ImageUrl)
                .SetProperty(b => b.ImageMetadataId, request.ImageMetadataId)
                .SetProperty(b => b.LocationId, request.LocationId)
                .SetProperty(b => b.Updated, dateTime),
                cancellationToken);

        // Reload the updated box
        var updatedBox = await dbContext.Boxes
            .AsNoTracking()
            .FirstAsync(b => b.BoxId == request.BoxId, cancellationToken);

        return Result.Success(updatedBox);
    }   
}