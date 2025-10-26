using Ardalis.Result;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Handlers.Images;

public record GetImageFile(Guid ImageId, string HashedUserId, string UserId) : IRequest<Result<ImageMetadata>>;

public class GetImageFileHandler : IRequestHandler<GetImageFile, Result<ImageMetadata>>
{
    private readonly StorageLabelsDbContext _dbContext;
    private readonly ILogger<GetImageFileHandler> _logger;

    public GetImageFileHandler(
        StorageLabelsDbContext dbContext,
        ILogger<GetImageFileHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<ImageMetadata>> Handle(GetImageFile request, CancellationToken cancellationToken)
    {
        var image = await _dbContext.Images
            .Include(img => img.ReferencedByBoxes)
            .ThenInclude(box => box.Location)
            .Include(img => img.ReferencedByItems)
            .ThenInclude(item => item.Box)
            .ThenInclude(box => box.Location)
            .FirstOrDefaultAsync(img => img.ImageId == request.ImageId && img.HashedUserId == request.HashedUserId, cancellationToken);

        if (image == null)
        {
            _logger.LogImageNotFound(request.ImageId, request.UserId);
            return Result.NotFound("Image not found");
        }

        // Check if the user owns the image
        if (image.UserId == request.UserId)
        {
            if (!System.IO.File.Exists(image.StoragePath))
            {
                _logger.LogImageFileNotFound(request.ImageId, request.UserId);
                return Result.NotFound("Image file not found");
            }
            // Content-Type validation
            if (!image.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogImageInvalidContentType(request.ImageId, request.UserId);
                return Result.Error("Invalid content type");
            }
            _logger.LogImageServedToOwner(request.ImageId, request.UserId);
            return Result.Success(image);
        }

        // Check if user has access to any box referencing this image
        var userLocationIds = await _dbContext.UserLocations
            .Where(ul => ul.UserId == request.UserId && ul.AccessLevel > AccessLevels.None)
            .Select(ul => ul.LocationId)
            .ToListAsync(cancellationToken);

        bool hasBoxAccess = image.ReferencedByBoxes.Any(box => userLocationIds.Contains(box.LocationId));
        bool hasItemAccess = image.ReferencedByItems.Any(item => userLocationIds.Contains(item.Box.LocationId));

        if (hasBoxAccess || hasItemAccess)
        {
            if (!System.IO.File.Exists(image.StoragePath))
            {
                _logger.LogImageFileNotFound(request.ImageId, request.UserId);
                return Result.NotFound("Image file not found");
            }
            // Content-Type validation
            if (!image.ContentType.Equals("image/jpeg", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogImageInvalidContentType(request.ImageId, request.UserId);
                return Result.Error("Invalid content type");
            }
            _logger.LogImageServedToUserViaAccess(request.ImageId, request.UserId);
            return Result.Success(image);
        }

        _logger.LogImageForbiddenAccess(request.ImageId, request.UserId);
        return Result.Forbidden("Access denied");
    }
}
