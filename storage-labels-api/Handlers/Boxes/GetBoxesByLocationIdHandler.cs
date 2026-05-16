using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Boxes;

public record GetBoxesByLocationId(long LocationId, string UserId) : IStreamRequest<Box>;

public class GetBoxesByLocationIdHandler(StorageLabelsDbContext dbContext) : IStreamRequestHandler<GetBoxesByLocationId, Box>
{
    public IAsyncEnumerable<Box> Handle(GetBoxesByLocationId request, CancellationToken cancellationToken)
    {
        return dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.LocationId == request.LocationId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.View))
            .AsAsyncEnumerable();
    }
}
