using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.Search;

namespace StorageLabelsApi.Services;

/// <summary>
/// PostgreSQL-specific search implementation using pg_trgm extension for substring matching.
/// Uses trigram indexes with ILIKE for filtering and trigram similarity for relevance scoring.
/// Matches substrings anywhere in text (e.g., "est" matches "test", "best", "estimated").
/// </summary>
public class PostgreSqlSearchService(
    StorageLabelsDbContext dbContext,
    ILogger<PostgreSqlSearchService> logger) : ISearchService
{
    public async Task<SearchResultsInternal> SearchBoxesAndItemsAsync(
        string query,
        string userId,
        long? locationId,
        Guid? boxId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PostgreSQL trigram search: {Query} for user {UserId}", query, userId);

        // Split query into words for AND matching and pre-compute patterns
        var searchWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var searchPatterns = searchWords.Select(w => $"%{w}%").ToArray();

        // Build base query for user's accessible locations
        var accessibleLocationIds = dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == userId && ul.AccessLevel != AccessLevels.None)
            .Select(ul => ul.LocationId);

        // Build box query using ILIKE for substring matching (uses trigram indexes)
        var boxQuery = dbContext.Boxes
            .AsNoTracking()
            .Where(b => accessibleLocationIds.Contains(b.LocationId));

        // Apply word filters to boxes
        foreach (var pattern in searchPatterns)
        {
            boxQuery = boxQuery.Where(b =>
                EF.Functions.ILike(b.Name, pattern) ||
                EF.Functions.ILike(b.Code, pattern) ||
                EF.Functions.ILike(b.Description ?? "", pattern));
        }

        if (locationId.HasValue)
        {
            var locId = locationId.Value;
            boxQuery = boxQuery.Where(b => b.LocationId == locId);
        }

        // Build item query using ILIKE for substring matching (uses trigram indexes)
        var itemQuery = dbContext.Items
            .AsNoTracking()
            .Where(i => accessibleLocationIds.Contains(i.Box.LocationId));

        // Apply word filters to items
        foreach (var pattern in searchPatterns)
        {
            itemQuery = itemQuery.Where(i =>
                EF.Functions.ILike(i.Name, pattern) ||
                EF.Functions.ILike(i.Description ?? "", pattern));
        }

        if (locationId.HasValue)
        {
            var locId = locationId.Value;
            itemQuery = itemQuery.Where(i => i.Box.LocationId == locId);
        }

        if (boxId.HasValue)
        {
            var bId = boxId.Value;
            itemQuery = itemQuery.Where(i => i.BoxId == bId);
        }

        // Count total results sequentially
        int boxCount = 0;
        int itemCount;
        
        if (!boxId.HasValue)
        {
            boxCount = await boxQuery.CountAsync(cancellationToken);
        }
        itemCount = await itemQuery.CountAsync(cancellationToken);
        
        var totalResults = boxCount + itemCount;

        // Build combined query with trigram similarity scoring for relevance
        var boxResultsQuery = boxQuery
            .Select(b => new
            {
                Type = "box",
                BoxId = b.BoxId.ToString(),
                BoxName = b.Name,
                BoxCode = b.Code,
                ItemId = (string?)null,
                ItemName = (string?)null,
                ItemCode = (string?)null,
                LocationId = b.LocationId.ToString(),
                LocationName = b.Location.Name,
                // Combine trigram similarity scores (0.0 to 1.0) weighted by field importance
                Rank = (float)(
                    EF.Functions.TrigramsSimilarity(b.Name, query) * 3.0 +
                    EF.Functions.TrigramsSimilarity(b.Code, query) * 2.0 +
                    EF.Functions.TrigramsSimilarity(b.Description ?? "", query) * 1.0)
            });

        var itemResultsQuery = itemQuery
            .Select(i => new
            {
                Type = "item",
                BoxId = i.BoxId.ToString(),
                BoxName = i.Box.Name,
                BoxCode = i.Box.Code,
                ItemId = (string?)i.ItemId.ToString(),
                ItemName = (string?)i.Name,
                ItemCode = (string?)null,
                LocationId = i.Box.LocationId.ToString(),
                LocationName = i.Box.Location.Name,
                // Combine trigram similarity scores weighted by field importance
                Rank = (float)(
                    EF.Functions.TrigramsSimilarity(i.Name, query) * 3.0 +
                    EF.Functions.TrigramsSimilarity(i.Description ?? "", query) * 1.0)
            });

        // Combine queries and apply pagination at database level
        var skip = (pageNumber - 1) * pageSize;
        var combinedQuery = boxId.HasValue 
            ? itemResultsQuery 
            : boxResultsQuery.Concat(itemResultsQuery);

        // Materialize results before DbContext disposal
        var materializedResults = await combinedQuery
            .OrderByDescending(r => r.Rank)
            .Skip(skip)
            .Take(pageSize)
            .Select(r => new SearchResult(
                r.Type,
                r.Rank,
                r.BoxId,
                r.BoxName,
                r.BoxCode,
                r.ItemId,
                r.ItemName,
                r.ItemCode,
                r.LocationId,
                r.LocationName))
            .ToListAsync(cancellationToken);

        logger.LogDebug("PostgreSQL trigram search: returning results for page {PageNumber} (total: {TotalResults})",
            pageNumber, totalResults);

        return new SearchResultsInternal(materializedResults.ToAsyncEnumerable(), totalResults);
    }
}
