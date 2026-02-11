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
            .AsNoTracking()
            .Where(b => b.Code == request.Code)
            .Where(b => dbContext.UserLocations
                .Any(ul => ul.LocationId == b.LocationId && 
                          ul.UserId == request.UserId && 
                          ul.AccessLevel != AccessLevels.None))
            .Select(b => new SearchResultResponse(
                "box",
                1.0f, // Exact match
                b.BoxId.ToString(),
                b.Name,
                b.Code,
                null,
                null,
                null,
                b.LocationId.ToString(),
                b.Location.Name
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (box != null)
            return Result<SearchResultResponse>.Success(box);

        return Result<SearchResultResponse>.NotFound();
    }
}
