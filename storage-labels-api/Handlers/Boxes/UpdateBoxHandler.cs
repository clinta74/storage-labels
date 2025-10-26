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

        var result = dbContext.Boxes
            .Update(box with 
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                ImageMetadataId = request.ImageMetadataId,
                LocationId = request.LocationId,
                Updated = dateTime
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Entity);
    }   
}