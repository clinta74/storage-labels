using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.DTO.Search;

namespace StorageLabelsApi.Services;

/// <summary>
/// PostgreSQL-specific search implementation using full-text search (FTS) with ranking
/// </summary>
public class PostgreSqlSearchService(
    StorageLabelsDbContext dbContext,
    ILogger<PostgreSqlSearchService> logger) : ISearchService
{
    public async Task<SearchResultsResponseV2> SearchBoxesAndItemsAsync(
        string query,
        string userId,
        long? locationId,
        Guid? boxId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PostgreSQL FTS search: {Query} for user {UserId}", query, userId);

        // Build base query for user's accessible locations
        var accessibleLocationIds = dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == userId && ul.AccessLevel != AccessLevels.None)
            .Select(ul => ul.LocationId);

        // Combined query for boxes and items with ranking
        var boxResults = new List<SearchResultV2>();
        var itemResults = new List<SearchResultV2>();

        // Search boxes if not filtering by BoxId
        if (!boxId.HasValue)
        {
            var boxQuery = dbContext.Boxes
                .AsNoTracking()
                .Where(b => accessibleLocationIds.Contains(b.LocationId))
                .Where(b => EF.Functions.ToTsVector("english", b.Name + " " + b.Code + " " + (b.Description ?? ""))
                    .Matches(EF.Functions.PlainToTsQuery("english", query)));

            if (locationId.HasValue)
            {
                boxQuery = boxQuery.Where(b => b.LocationId == locationId.Value);
            }

            boxResults = await boxQuery
                .Select(b => new SearchResultV2
                {
                    Type = "box",
                    BoxId = b.BoxId.ToString(),
                    BoxName = b.Name,
                    BoxCode = b.Code,
                    ItemId = null,
                    ItemName = null,
                    ItemCode = null,
                    LocationId = b.LocationId.ToString(),
                    LocationName = b.Location.Name,
                    Rank = EF.Functions.ToTsVector("english", b.Name + " " + b.Code + " " + (b.Description ?? ""))
                        .Rank(EF.Functions.PlainToTsQuery("english", query))
                })
                .ToListAsync(cancellationToken);
        }

        // Search items
        var itemQuery = dbContext.Items
            .AsNoTracking()
            .Where(i => accessibleLocationIds.Contains(i.Box.LocationId))
            .Where(i => EF.Functions.ToTsVector("english", i.Name + " " + (i.Description ?? ""))
                .Matches(EF.Functions.PlainToTsQuery("english", query)));

        if (locationId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.Box.LocationId == locationId.Value);
        }

        if (boxId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.BoxId == boxId.Value);
        }

        itemResults = await itemQuery
            .Select(i => new SearchResultV2
            {
                Type = "item",
                BoxId = i.BoxId.ToString(),
                BoxName = i.Box.Name,
                BoxCode = i.Box.Code,
                ItemId = i.ItemId.ToString(),
                ItemName = i.Name,
                ItemCode = null,
                LocationId = i.Box.LocationId.ToString(),
                LocationName = i.Box.Location.Name,
                Rank = EF.Functions.ToTsVector("english", i.Name + " " + (i.Description ?? ""))
                    .Rank(EF.Functions.PlainToTsQuery("english", query))
            })
            .ToListAsync(cancellationToken);

        // Combine and sort by rank (descending)
        var allResults = boxResults
            .Concat(itemResults)
            .OrderByDescending(r => r.Rank)
            .ToList();

        var totalResults = allResults.Count;

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        var pagedResults = allResults
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        logger.LogDebug("PostgreSQL FTS search completed: {TotalResults} results, {PagedResults} on page {PageNumber}",
            totalResults, pagedResults.Count, pageNumber);

        return new SearchResultsResponseV2
        {
            Results = pagedResults,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalResults = totalResults
        };
    }
}
