using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Items;

public record DeleteItem(Guid ItemId, string? UserId) : IRequest<Result>;

public class DeleteItemHandler(StorageLabelsDbContext dbContext) : IRequestHandler<DeleteItem, Result>
{
    public async Task<Result> Handle(DeleteItem request, CancellationToken cancellationToken)
    {
        var item = await dbContext.Items
            .Where(i => i.ItemId == request.ItemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.Edit))
            .FirstOrDefaultAsync(cancellationToken);

        if (item is null)
            return Result.NotFound();

        dbContext.Items.Remove(item);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}