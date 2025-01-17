using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record GetLocationsByUserId(string UserId) : IRequest<Result<List<Location>>>;
public class GetLocationsByUserIdHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetLocationsByUserId, Result<List<Location>>>
{
    public async Task<Result<List<Location>>> Handle(GetLocationsByUserId request, CancellationToken cancellationToken)
    {
        var locations = await dbContext.UserLocations
            .AsNoTracking()
            .Where(l => l.UserId == request.UserId)
            .Where(ul => ul.AccessLevel > AccessLevels.None)
            .Select(ul => ul.Location)
            .ToListAsync(cancellationToken);

        return Result.Success(locations);
    }
}
