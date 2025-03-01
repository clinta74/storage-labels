
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record DeleteLocation(string UserId, long LocationId) : IRequest<Result>;

public class DeleteLocationHandler(StorageLabelsDbContext dbContext, ILogger<DeleteLocationHandler> logger) : IRequestHandler<DeleteLocation, Result>
{
    public async Task<Result> Handle(DeleteLocation request, CancellationToken cancellationToken)
    {

        var location = await dbContext.Locations
            .AsNoTracking()
            .Where(l => l.LocationId == request.LocationId)
            .Where(l => l.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel == AccessLevels.Owner))
            .Include(l => l.Boxes)
            .FirstOrDefaultAsync(cancellationToken);

        if (location is null)
        {
            return Result.NotFound($"Location with id {request.LocationId} was not found.");
        }

        if (location.Boxes.Count > 0)
        {
            return Result.Invalid(location.Boxes.Select(box => new ValidationError
            {
                Identifier = nameof(Location),
                ErrorCode = "LocationStillInUse",
                ErrorMessage = $"Location id ({location.LocationId}) with name ({location.Name}) has box id ({box.BoxId}).",
                Severity = ValidationSeverity.Info,
            }));
        }

        dbContext.Locations.Remove(location);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.DeleteLocation(request.LocationId);

        return Result.Success();
    }

}
