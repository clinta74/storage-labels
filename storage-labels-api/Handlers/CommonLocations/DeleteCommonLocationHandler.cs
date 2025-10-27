
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Handlers.CommonLocations;

public record DeleteCommonLocation(int CommonLocationId) : IRequest<Result>;

public class DeleteCommonLocationHandler(StorageLabelsDbContext dbContext) : IRequestHandler<DeleteCommonLocation, Result>
{
    public async Task<Result> Handle(DeleteCommonLocation request, CancellationToken cancellationToken)
    {
        var exists = await dbContext.CommonLocations
            .AsNoTracking()
            .Where(cl => cl.CommonLocationId == request.CommonLocationId)
            .AnyAsync(cancellationToken);
        
        if (!exists)
        {
            return Result.NotFound($"Common location id {request.CommonLocationId} not found.");
        }

        await dbContext.CommonLocations
            .Where(cl => cl.CommonLocationId == request.CommonLocationId)
            .ExecuteDeleteAsync(cancellationToken);

        return Result.Success();
    }
}
