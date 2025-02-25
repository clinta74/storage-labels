using Ardalis.Result.FluentValidation;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Boxes;
public record CreateBox(string Code, string Name, string UserId, long LocationId, string? Description = null, string? ImageUrl = null) : IRequest<Result<Box>>;

public class CreateBoxHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider, ILogger<CreateBoxHandler> logger) : IRequestHandler<CreateBox, Result<Box>>
{
    public async Task<Result<Box>> Handle(CreateBox request, CancellationToken cancellationToken)
    {
        var validation = await new CreateBoxValidator().ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Result<Box>.Invalid(validation.AsErrors());
        }

        var hasBoxCode = await dbContext.Boxes
            .AsNoTracking()
            .Where(box => box.Code == request.Code)
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

        var dateTime = timeProvider.GetUtcNow();

        var box = dbContext.Boxes
            .Add(new(
                BoxId: Guid.CreateVersion7(),
                Code: request.Code,
                Name: request.Name,
                Description: request.Description,
                ImageUrl: request.ImageUrl,
                LocationId: request.LocationId,
                Created: dateTime,
                Updated: dateTime,
                LastAccessed: dateTime)
            );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Created(box.Entity);
    }
}