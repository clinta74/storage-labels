using Ardalis.Result;
using Mediator;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Search;

namespace StorageLabelsApi.Handlers.Search;

public record SearchBoxesAndItemsQuery(string Query, string UserId, long? LocationId, Guid? BoxId) : IRequest<Result<SearchResultsResponse>>;

public class SearchBoxesAndItemsHandler(
    StorageLabelsDbContext dbContext,
    ILogger<SearchBoxesAndItemsHandler> logger) : IRequestHandler<SearchBoxesAndItemsQuery, Result<SearchResultsResponse>>
{
    public async ValueTask<Result<SearchResultsResponse>> Handle(SearchBoxesAndItemsQuery request, CancellationToken cancellationToken)
    {
        logger.LegacySearchStarted(request.Query, request.UserId, request.LocationId, request.BoxId);
        
        var searchTerm = request.Query.ToLower();
        var results = new List<SearchResultResponse>();

        // Build base query for user's accessible locations
        var accessibleLocationIds = dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == request.UserId && ul.AccessLevel != AccessLevels.None)
            .Select(ul => ul.LocationId);

        // Search boxes
        var boxQuery = dbContext.Boxes
            .AsNoTracking()
            .Where(b => accessibleLocationIds.Contains(b.LocationId));

        // Filter by location if specified
        if (request.LocationId.HasValue)
        {
            boxQuery = boxQuery.Where(b => b.LocationId == request.LocationId.Value);
        }

        // Filter by box if specified (for items only, skip boxes in this case)
        if (!request.BoxId.HasValue)
        {
            var boxes = await boxQuery
                .Where(b => EF.Functions.Like(b.Name.ToLower(), $"%{searchTerm}%") ||
                           EF.Functions.Like(b.Code.ToLower(), $"%{searchTerm}%") ||
                           (b.Description != null && EF.Functions.Like(b.Description.ToLower(), $"%{searchTerm}%")))
                .Select(b => new SearchResultResponse
                {
                    Type = "box",
                    BoxId = b.BoxId.ToString(),
                    BoxName = b.Name,
                    BoxCode = b.Code,
                    LocationId = b.LocationId.ToString(),
                    LocationName = b.Location.Name
                })
                .Take(20)
                .ToListAsync(cancellationToken);

            results.AddRange(boxes);
        }

        // Search items
        var itemQuery = dbContext.Items
            .AsNoTracking()
            .Where(i => accessibleLocationIds.Contains(i.Box.LocationId));

        // Filter by location if specified
        if (request.LocationId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.Box.LocationId == request.LocationId.Value);
        }

        // Filter by box if specified
        if (request.BoxId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.BoxId == request.BoxId.Value);
        }

        var items = await itemQuery
            .Where(i => EF.Functions.Like(i.Name.ToLower(), $"%{searchTerm}%") ||
                       (i.Description != null && EF.Functions.Like(i.Description.ToLower(), $"%{searchTerm}%")))
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
            .Take(20)
            .ToListAsync(cancellationToken);

        results.AddRange(items);

        logger.LegacySearchCompleted(request.Query, results.Count);
        
        return Result<SearchResultsResponse>.Success(new SearchResultsResponse { Results = results });
    }
}
