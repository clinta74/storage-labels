using MediatR;
using Microsoft.Extensions.Logging;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Images;

public record DeleteImage(Guid ImageId, string UserId) : IRequest<Result>;

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

    public async Task<Result> Handle(DeleteImage request, CancellationToken cancellationToken)
    {
        var image = await _dbContext.Images.FindAsync([request.ImageId], cancellationToken);
        if (image == null)
        {
            return Result.NotFound("Image not found");
        }

        if (image.UserId != request.UserId)
        {
            return Result.Unauthorized("Not authorized to delete this image");
        }

        if (image.ReferencedByBoxes.Any() || image.ReferencedByItems.Any())
        {
            return Result.Error("Cannot delete image that is still referenced by boxes or items");
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

        _dbContext.Images.Remove(image);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogImageDeleted(image.ImageId, request.UserId);

        return Result.Success();
    }
}
