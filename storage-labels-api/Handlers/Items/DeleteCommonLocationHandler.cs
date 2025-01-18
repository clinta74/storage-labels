
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Handlers.Items;

public record DeleteCommonLocation(int CommonLocationId) : IRequest<Result>;

public class DeleteCommonLocationHandler(StorageLabelsDbContext dbContext) : IRequestHandler<DeleteCommonLocation, Result>
{
    public async Task<Result> Handle(DeleteCommonLocation request, CancellationToken cancellationToken)
    {
        var commonLocation = await dbContext.CommonLocations
            .AsNoTracking()
            .Where(cl => cl.CommonLocationId == request.CommonLocationId)
            .FirstOrDefaultAsync(cancellationToken);
        
        if (commonLocation is null)
        {
            return Result.NotFound($"Common location id {request.CommonLocationId} not found.");
        }

        return Result.Success();
    }
}
