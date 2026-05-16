using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Item;

namespace StorageLabelsApi.Endpoints.Items;

internal static partial class ItemEndpoints
{
    private static async Task<Results<Ok<ItemResponse>, NotFound, ValidationProblem>> UpdateItem(HttpContext context, [FromRoute] Guid itemId, ItemRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var item = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.ItemId == itemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == userId && ul.AccessLevel >= AccessLevels.Edit))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return TypedResults.NotFound();

        var validation = await new UpdateItemValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return TypedResults.ValidationProblem(validation.ToDictionary());

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Items
            .Where(i => i.ItemId == itemId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(i => i.BoxId, request.BoxId)
                .SetProperty(i => i.Name, request.Name)
                .SetProperty(i => i.Description, request.Description)
                .SetProperty(i => i.ImageUrl, request.ImageUrl)
                .SetProperty(i => i.ImageMetadataId, request.ImageMetadataId)
                .SetProperty(i => i.Updated, dateTime),
                cancellationToken);

        var updatedItem = await dbContext.Items
            .AsNoTracking()
            .FirstAsync(i => i.ItemId == itemId, cancellationToken);

        return TypedResults.Ok(new ItemResponse(updatedItem));
    }

    private sealed class UpdateItemValidator : AbstractValidator<ItemRequest>
    {
        public UpdateItemValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
