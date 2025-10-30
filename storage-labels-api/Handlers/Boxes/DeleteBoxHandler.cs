using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Boxes;

public record DeleteBox(Guid BoxId, string UserId, bool Force = false) : IRequest<Result>;

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

        if (box.Items.Count > 0 && !request.Force)
        {
            return Result.Invalid(box.Items.Select(item => new ValidationError
            {
                Identifier = nameof(Box),
                ErrorCode = "BoxStillInUse",
                ErrorMessage = $"Box id ({box.BoxId}) with name ({box.Name}) with item id ({item.ItemId}).",
                Severity = ValidationSeverity.Info,
            }));
        }

        // If force delete, delete items first
        if (request.Force && box.Items.Count > 0)
        {
            await dbContext.Items
                .Where(i => i.BoxId == request.BoxId)
                .ExecuteDeleteAsync(cancellationToken);
        }

        await dbContext.Boxes
            .Where(b => b.BoxId == request.BoxId)
            .ExecuteDeleteAsync(cancellationToken);

        logger.DeleteBox(request.BoxId);

        return Result.Success();
    }

    
}
