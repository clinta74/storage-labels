using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Images;

public record DeleteImage(Guid ImageId, string UserId, bool Force) : IRequest<Result>;

public class DeleteImageHandler : IRequestHandler<DeleteImage, Result>
{
    private readonly StorageLabelsDbContext _dbContext;
    private readonly ILogger<DeleteImageHandler> _logger;

    public DeleteImageHandler(
        StorageLabelsDbContext dbContext,
        ILogger<DeleteImageHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async ValueTask<Result> Handle(DeleteImage request, CancellationToken cancellationToken)
    {
        var image = await _dbContext.Images
            .AsNoTracking()
            .Include(i => i.ReferencedByBoxes)
            .Include(i => i.ReferencedByItems)
            .FirstOrDefaultAsync(i => i.ImageId == request.ImageId, cancellationToken);
            
        if (image == null)
        {
            return Result.NotFound("Image not found");
        }

        if (image.UserId != request.UserId)
        {
            return Result.Unauthorized("Not authorized to delete this image");
        }

        var hasReferences = image.ReferencedByBoxes.Count != 0 || image.ReferencedByItems.Count != 0;
        
        if (hasReferences && !request.Force)
        {
            var boxCount = image.ReferencedByBoxes.Count;
            var itemCount = image.ReferencedByItems.Count;
            return Result.Error($"Cannot delete image that is still referenced by {boxCount} box(es) and {itemCount} item(s). Use force delete to remove references.");
        }

        // If forcing delete, clear all references
        if (request.Force && hasReferences)
        {
            // Clear image references from boxes
            await _dbContext.Boxes
                .Where(b => b.ImageMetadataId == request.ImageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(b => b.ImageUrl, (string?)null)
                    .SetProperty(b => b.ImageMetadataId, (Guid?)null),
                    cancellationToken);
                
            // Clear image references from items
            await _dbContext.Items
                .Where(i => i.ImageMetadataId == request.ImageId)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.ImageUrl, (string?)null)
                    .SetProperty(i => i.ImageMetadataId, (Guid?)null),
                    cancellationToken);
        }

        try
        {
            if (File.Exists(image.StoragePath))
            {
                File.Delete(image.StoragePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogImageDeleteError(ex, image.ImageId);
        }

        await _dbContext.Images
            .Where(i => i.ImageId == request.ImageId)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogImageDeleted(image.ImageId, request.UserId);

        return Result.Success();
    }
}
