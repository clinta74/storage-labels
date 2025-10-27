using MediatR;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Models.DTO;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Images;

public record GetUserImages(string UserId) : IRequest<Result<List<ImageMetadata>>>;

public class GetUserImagesHandler : IRequestHandler<GetUserImages, Result<List<ImageMetadata>>>
{
    private readonly StorageLabelsDbContext _dbContext;

    public GetUserImagesHandler(StorageLabelsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<List<ImageMetadata>>> Handle(GetUserImages request, CancellationToken cancellationToken)
    {
        var images = await _dbContext.Images
            .AsNoTracking()
            .Include(img => img.ReferencedByBoxes)
            .Include(img => img.ReferencedByItems)
            .Where(img => img.UserId == request.UserId)
            .ToListAsync(cancellationToken);

        return Result.Success(images);
    }
}
