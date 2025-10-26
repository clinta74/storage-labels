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
            .Where(i => i.ItemId == request.ItemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.Edit))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.NotFound();

        // Validate
        var validation = await new UpdateItemValidator().ValidateAsync(request);
        if (!validation.IsValid)
            return Result<Item>.Invalid(validation.AsErrors());

        // Use 'with' expression for mapping
        item = item with
        {
            BoxId = request.BoxId,
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            ImageMetadataId = request.ImageMetadataId,
            Updated = timeProvider.GetUtcNow()
        };

        dbContext.Items.Update(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(item);
    }
}