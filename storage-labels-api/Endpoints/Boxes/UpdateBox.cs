using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Box;

namespace StorageLabelsApi.Endpoints.Boxes;

internal partial class BoxEndpoints
{
    private static async Task<Results<Ok<BoxResponse>, NotFound<string>, ValidationProblem, ProblemHttpResult>> UpdateBox(HttpContext context, [FromRoute] Guid boxId, BoxRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, [FromServices] ILogger<BoxEndpoints> logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var validation = await new UpdateBoxValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var hasBoxCodeInLocation = await dbContext.Boxes
            .AsNoTracking()
            .Where(box => box.Code == request.Code)
            .Where(box => box.LocationId == request.LocationId)
            .Where(box => box.BoxId != boxId)
            .AnyAsync(cancellationToken);

        if (hasBoxCodeInLocation)
        {
            return TypedResults.Problem($"A box with the code {request.Code} already exists in this location", statusCode: 409);
        }

        var userCanAccessLocation = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.LocationId == request.LocationId)
            .Where(ul => ul.UserId == userId)
            .Where(ul => ul.AccessLevel >= AccessLevels.Edit)
            .AnyAsync(cancellationToken);

        if (!userCanAccessLocation)
        {
            logger.NoAccessToLocation(userId, request.LocationId);
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { [nameof(Location)] = [$"User cannot add box to location ({request.LocationId})."] });
        }

        var exists = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == boxId)
            .AnyAsync(cancellationToken);

        if (!exists)
        {
            return TypedResults.NotFound($"Box with id {boxId} not found.");
        }

        var dateTime = timeProvider.GetUtcNow();

        await dbContext.Boxes
            .Where(b => b.BoxId == boxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(b => b.Code, request.Code)
                .SetProperty(b => b.Name, request.Name)
                .SetProperty(b => b.Description, request.Description)
                .SetProperty(b => b.ImageUrl, request.ImageUrl)
                .SetProperty(b => b.ImageMetadataId, request.ImageMetadataId)
                .SetProperty(b => b.LocationId, request.LocationId)
                .SetProperty(b => b.Updated, dateTime),
                cancellationToken);

        var updatedBox = await dbContext.Boxes
            .AsNoTracking()
            .FirstAsync(b => b.BoxId == boxId, cancellationToken);

        return TypedResults.Ok(new BoxResponse(updatedBox));
    }

    private sealed class UpdateBoxValidator : AbstractValidator<BoxRequest>
    {
        public UpdateBoxValidator()
        {
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
