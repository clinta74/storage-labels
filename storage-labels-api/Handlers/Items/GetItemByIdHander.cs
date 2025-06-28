using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Items;

public record GetItemById(Guid ItemId, string? UserId) : IRequest<Result<Item>>;

public class GetItemByIdHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetItemById, Result<Item>>
{
    public async Task<Result<Item>> Handle(GetItemById request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .Where(i => i.ItemId == request.ItemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.View))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.NotFound();

        return Result.Success(item);
    }
}