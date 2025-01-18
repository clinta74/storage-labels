using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Items;

public record GetItemsByBoxId(Guid BoxId, string UserId) : IStreamRequest<Item>;

public class GetItemsByBoxIdHander(StorageLabelsDbContext dbContext) : IStreamRequestHandler<GetItemsByBoxId, Item>
{
    public IAsyncEnumerable<Item> Handle(GetItemsByBoxId request, CancellationToken cancellationToken)
    {
        return dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.View))
            .Include(b => b.Items)
            .SelectMany(b => b.Items)
            .Where(b => b.BoxId == request.BoxId)
            .AsAsyncEnumerable();
    }
}
