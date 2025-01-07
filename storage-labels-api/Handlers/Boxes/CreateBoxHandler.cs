using Ardalis.Result.FluentValidation;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Boxes;
public record CreateBox(string Code, string Name, string UserId, long LocationId, string? Description = null, string? ImageUrl = null) : IRequest<Result<Box>>;

public class CreateBoxHandler(StorageLabelsDbContext dbContext, ILogger<CreateBoxHandler> logger) : IRequestHandler<CreateBox, Result<Box>>
{
    public async Task<Result<Box>> Handle(CreateBox request, CancellationToken cancellationToken)
    {
        var validation = await new CreateBoxValidator().ValidateAsync(request);
        if (!validation.IsValid)
        {
            logger.LogWarning("Create Box failed validation: {validation}", validation);
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
            .Where(userLocation => userLocation.AccessLevel == AccessLevels.Edit || userLocation.AccessLevel == AccessLevels.Owner)
            .AnyAsync(cancellationToken);

         if (hasBoxCode)
        {
            return Result.Forbidden([$"User ({request.UserId}) cannot add box to location ({request.LocationId})."]);
        }

        var dateTime = DateTimeOffset.UtcNow;

        var box = dbContext.Boxes
            .Add(new()
            {
                BoxId = Guid.CreateVersion7(),
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                LocationId = request.LocationId,
                Created = dateTime,
                Updated = dateTime,
                Access = dateTime,
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Created(box.Entity);
    }
}