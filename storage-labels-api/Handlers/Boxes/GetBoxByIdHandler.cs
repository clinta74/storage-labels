using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Boxes;

public record GetBoxById(Guid BoxId, string UserId) : IRequest<Result<Box>>;
public class GetBoxByIdHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetBoxById, Result<Box>>
{
    public async Task<Result<Box>> Handle(GetBoxById request, CancellationToken cancellationToken)
    {
        var box = await dbContext.Boxes
            .AsNoTracking()
            .Where(b => b.BoxId == request.BoxId)
            .Where(b => b.Location.UserLocations.Any(ul => ul.UserId == request.UserId))
            .Where(b => b.Location.UserLocations.Where(ul => ul.UserId == request.UserId).First().AccessLevel > AccessLevels.None)
            .FirstOrDefaultAsync(cancellationToken);

        if (box is null)
        {
            return Result.NotFound($"Box with id {request.BoxId } was not found.");
        }

        return Result.Success(box);
    }
}
