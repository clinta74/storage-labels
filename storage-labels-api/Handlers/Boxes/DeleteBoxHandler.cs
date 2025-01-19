using System.Runtime.InteropServices;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Boxes;

public record DeleteBox(Guid BoxId, string UserId) : IRequest<Result>;

public class DeleteBoxHandler(StorageLabelsDbContext dbContext, ILogger<DeleteBoxHandler> logger) : IRequestHandler<DeleteBox, Result>
{
    public async Task<Result> Handle(DeleteBox request, CancellationToken cancellationToken)
    {
        var box = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == request.BoxId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == request.UserId && ul.AccessLevel == AccessLevels.Owner))
            .FirstOrDefaultAsync(cancellationToken);
        if (box is null)
        {
            return Result.NotFound($"Box with id ({request.BoxId}) ");
        }

        dbContext.Boxes.Remove(box);

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
