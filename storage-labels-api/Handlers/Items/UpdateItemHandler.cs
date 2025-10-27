using Ardalis.Result.FluentValidation;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Items;

public record UpdateItem(
    Guid ItemId,
    string? UserId,
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId
) : IRequest<Result<Item>>;

public class UpdateItemHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<UpdateItem, Result<Item>>
{
    public async Task<Result<Item>> Handle(UpdateItem request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.ItemId == request.ItemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.Edit))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.NotFound();

        // Validate
        var validation = await new UpdateItemValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return Result<Item>.Invalid(validation.AsErrors());

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Items
            .Where(i => i.ItemId == request.ItemId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.BoxId, request.BoxId)
                .SetProperty(i => i.Name, request.Name)
                .SetProperty(i => i.Description, request.Description)
                .SetProperty(i => i.ImageUrl, request.ImageUrl)
                .SetProperty(i => i.ImageMetadataId, request.ImageMetadataId)
                .SetProperty(i => i.Updated, dateTime),
                cancellationToken);

        // Reload the updated item
        var updatedItem = await dbContext.Items
            .AsNoTracking()
            .FirstAsync(i => i.ItemId == request.ItemId, cancellationToken);

        return Result.Success(updatedItem);
    }
}