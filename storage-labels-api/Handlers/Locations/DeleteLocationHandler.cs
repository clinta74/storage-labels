
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Locations;

public record DeleteLocation(string UserId, long LocationId, bool Force = false) : IRequest<Result>;

public class DeleteLocationHandler(StorageLabelsDbContext dbContext, ILogger<DeleteLocationHandler> logger) : IRequestHandler<DeleteLocation, Result>
{
    public async ValueTask<Result> Handle(DeleteLocation request, CancellationToken cancellationToken)
    {

        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == request.LocationId)
            .Where(l => l.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel == AccessLevels.Owner))
            .Include(l => l.Boxes)
                .ThenInclude(b => b.Items)
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            return Result.NotFound($"Location with id {request.LocationId} was not found.");
        }

        if (location.Boxes.Count > 0 && !request.Force)
        {
            return Result.Invalid(location.Boxes.Select(box => new ValidationError
            {
                Identifier = nameof(Location),
                ErrorCode = "LocationStillInUse",
                ErrorMessage = $"Location id ({location.LocationId}) with name ({location.Name}) has box id ({box.BoxId}).",
                Severity = ValidationSeverity.Info,
            }));
        }

        // If force delete, delete boxes and their items first
        if (request.Force && location.Boxes.Count > 0)
        {
            // Delete all items in all boxes
            var boxIds = location.Boxes.Select(b => b.BoxId).ToList();
            await dbContext.Items
                .Where(i => boxIds.Contains(i.BoxId))
                .ExecuteDeleteAsync(cancellationToken);
            
            // Delete all boxes
            await dbContext.Boxes
                .Where(b => b.LocationId == request.LocationId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await dbContext.Locations
            .Where(l => l.LocationId == request.LocationId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.DeleteLocation(request.LocationId);

        return Result.Success();
    }

}
