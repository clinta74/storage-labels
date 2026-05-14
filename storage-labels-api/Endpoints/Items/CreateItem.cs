using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Item;

namespace StorageLabelsApi.Endpoints.Items;

internal static partial class ItemEndpoints
{
    private static async Task<Results<Created<ItemResponse>, ValidationProblem>> CreateItem(HttpContext context, ItemRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var validation = await new CreateItemValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var userCanEditBox = await dbContext.UserLocations
            .Where(ul => ul.UserId == userId)
            .Where(ul => ul.AccessLevel >= AccessLevels.Edit)
            .Where(ul => ul.Location.Boxes.Any(b => b.BoxId == request.BoxId))
            .AnyAsync(cancellationToken);

        if (!userCanEditBox)
        {
            logger.LogItemAddAttemptWarning(userId, request.BoxId);
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(Box)] = [$"Cannot add item to box ({request.BoxId})."] });
        }

        var dateTime = timeProvider.GetUtcNow();
        var item = dbContext.Items.Add(new(
            ItemId: Guid.CreateVersion7(),
            BoxId: request.BoxId,
            Name: request.Name,
            Description: request.Description,
            ImageUrl: request.ImageUrl,
            ImageMetadataId: request.ImageMetadataId,
            Created: dateTime,
            Updated: dateTime
        ));

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created((string?)null, new ItemResponse(item.Entity));
    }

    private sealed class CreateItemValidator : AbstractValidator<ItemRequest>
    {
        public CreateItemValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
