using Ardalis.Result.FluentValidation;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Items;

public record CreateItem(  
    string? UserId,  
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl
) : IRequest<Result<Item>>;

public class CreateItemHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider, ILogger<CreateItemHandler> logger) : IRequestHandler<CreateItem, Result<Item>>
{
    public async Task<Result<Item>> Handle(CreateItem request, CancellationToken cancellationToken)
    {
        var userCanEditBox = await dbContext.UserLocations
            .Where(ul => ul.UserId == request.UserId)
            .Where(ul => ul.AccessLevel >= AccessLevels.Edit)
            .Where(ul => ul.Location.Boxes.Any(b => b.BoxId == request.BoxId))
            .AnyAsync(cancellationToken);

        if (!userCanEditBox)
        {
            logger.LogWarning("User attempted to add an item {request} to box without access", request);
            return Result<Item>.NotFound("Box not found or invalid permission.");
        }

        var validation = await new ItemValidator().ValidateAsync(request);
        if (!validation.IsValid)
        {
            logger.LogWarning("Create Box failed validation: {validation}", validation);
            return Result<Item>.Invalid(validation.AsErrors());
        }

        var dateTime = timeProvider.GetUtcNow();
        var item = dbContext.Items.Add(
            new (
                ItemId: Guid.CreateVersion7(),
                BoxId: request.BoxId,
                Name: request.Name,
                Description: request.Description,
                ImageUrl: request.ImageUrl,
                Created: dateTime,
                Updated: dateTime
            )
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(item.Entity);
    }
}
