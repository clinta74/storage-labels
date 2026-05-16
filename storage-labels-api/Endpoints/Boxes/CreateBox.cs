using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Box;

namespace StorageLabelsApi.Endpoints.Boxes;

internal static partial class BoxEndpoints
{
    private static async Task<Results<Created<BoxResponse>, ValidationProblem, ProblemHttpResult>> CreateBox(HttpContext context, BoxRequest request, [FromServices] StorageLabelsDbContext dbContext, [FromServices] TimeProvider timeProvider, ILogger logger, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var validation = await new CreateBoxValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return TypedResults.ValidationProblem(validation.ToDictionary());
        }

        var hasBoxCodeInLocation = await dbContext.Boxes
            .AsNoTracking()
            .Where(box => box.Code == request.Code)
            .Where(box => box.LocationId == request.LocationId)
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

        var dateTime = timeProvider.GetUtcNow();

        var box = dbContext.Boxes.Add(new(
            BoxId: Guid.CreateVersion7(),
            Code: request.Code,
            Name: request.Name,
            Description: request.Description,
            ImageUrl: request.ImageUrl,
            ImageMetadataId: request.ImageMetadataId,
            LocationId: request.LocationId,
            Created: dateTime,
            Updated: dateTime,
            LastAccessed: dateTime)
        );

        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created((string?)null, new BoxResponse(box.Entity));
    }

    private sealed class CreateBoxValidator : AbstractValidator<BoxRequest>
    {
        public CreateBoxValidator()
        {
            RuleFor(x => x.Code).NotEmpty();
            RuleFor(x => x.Name).NotEmpty();
        }
    }
}
