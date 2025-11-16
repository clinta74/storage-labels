using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Search;

namespace StorageLabelsApi.Handlers.Search;

public record SearchByQrCodeQuery(string Code, string UserId) : IRequest<Result<SearchResultResponse>>;

public class SearchByQrCodeHandler : IRequestHandler<SearchByQrCodeQuery, Result<SearchResultResponse>>
{
    private readonly StorageLabelsDbContext dbContext;

    public SearchByQrCodeHandler(StorageLabelsDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async ValueTask<Result<SearchResultResponse>> Handle(SearchByQrCodeQuery request, CancellationToken cancellationToken)
    {
        // Search for exact match in boxes the user has access to
        var box = await dbContext.Boxes
            .Where(b => b.Code == request.Code)
            .Where(b => dbContext.UserLocations
                .Any(ul => ul.LocationId == b.LocationId && 
                          ul.UserId == request.UserId && 
                          ul.AccessLevel != AccessLevels.None))
            .Select(b => new SearchResultResponse
            {
                Type = "box",
                BoxId = b.BoxId.ToString(),
                BoxName = b.Name,
                BoxCode = b.Code,
                LocationId = b.LocationId.ToString(),
                LocationName = b.Location.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (box != null)
            return Result<SearchResultResponse>.Success(box);

        // Search for exact match in items by name (items don't have codes)
        // This is a fallback - QR codes typically map to boxes
        var item = await dbContext.Items
            .Where(i => dbContext.UserLocations
                .Any(ul => ul.LocationId == i.Box.LocationId && 
                          ul.UserId == request.UserId && 
                          ul.AccessLevel != AccessLevels.None))
            .Where(i => i.Name.ToLower() == request.Code.ToLower())
            .Select(i => new SearchResultResponse
            {
                Type = "item",
                ItemId = i.ItemId.ToString(),
                ItemName = i.Name,
                ItemCode = null,
                BoxId = i.BoxId.ToString(),
                BoxName = i.Box.Name,
                BoxCode = i.Box.Code,
                LocationId = i.Box.LocationId.ToString(),
                LocationName = i.Box.Location.Name
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (item != null)
            return Result<SearchResultResponse>.Success(item);

        return Result<SearchResultResponse>.NotFound();
    }
}
