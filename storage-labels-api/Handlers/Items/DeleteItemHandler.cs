using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Items;

public record DeleteItem(Guid ItemId, string? UserId) : IRequest<Result>;

public class DeleteItemHandler(StorageLabelsDbContext dbContext) : IRequestHandler<DeleteItem, Result>
{
    public async ValueTask<Result> Handle(DeleteItem request, CancellationToken cancellationToken)
    {
        var hasAccess = await dbContext.Items
            .AsNoTracking()
            .Where(i => i.ItemId == request.ItemId)
            .Where(i => i.Box.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel >= AccessLevels.Edit))
            .AnyAsync(cancellationToken);

        if (!hasAccess)
            return Result.NotFound();

        await dbContext.Items
            .Where(i => i.ItemId == request.ItemId)
            .ExecuteDeleteAsync(cancellationToken);

        return Result.Success();
    }
}