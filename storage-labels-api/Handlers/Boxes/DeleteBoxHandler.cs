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
            .Include(b => b.Items)
            .FirstOrDefaultAsync(cancellationToken);
            
        if (box is null)
        {
            return Result.NotFound($"Box with id ({request.BoxId})");
        }

        if (box.Items.Count > 0)
        {
            return Result.Invalid(box.Items.Select(item => new ValidationError
            {
                Identifier = nameof(Box),
                ErrorCode = "BoxStillInUse",
                ErrorMessage = $"Box id ({box.BoxId}) with name ({box.Name}) with item id ({item.ItemId}).",
                Severity = ValidationSeverity.Info,
            }));
        }


        dbContext.Boxes.Remove(box);

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.DeleteBox(request.BoxId);

        return Result.Success();
    }

    
}
