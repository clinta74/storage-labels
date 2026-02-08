using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.Search;

namespace StorageLabelsApi.Services;

/// <summary>
/// PostgreSQL-specific search implementation using full-text search (FTS) with ranking
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
        logger.LogDebug("PostgreSQL FTS search: {Query} for user {UserId}", query, userId);

        // Build base query for user's accessible locations
        var accessibleLocationIds = dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == userId && ul.AccessLevel != AccessLevels.None)
            .Select(ul => ul.LocationId);

        // Build reusable filter expressions
        Expression<Func<Box, bool>> boxBaseFilter = b => 
            accessibleLocationIds.Contains(b.LocationId) &&
            EF.Functions.ToTsVector("english", b.Name + " " + b.Code + " " + (b.Description ?? ""))
                .Matches(EF.Functions.PlainToTsQuery("english", query));

        Expression<Func<Item, bool>> itemBaseFilter = i => 
            accessibleLocationIds.Contains(i.Box.LocationId) &&
            EF.Functions.ToTsVector("english", i.Name + " " + (i.Description ?? ""))
                .Matches(EF.Functions.PlainToTsQuery("english", query));

        // Build box query with filters
        var boxQuery = dbContext.Boxes
            .AsNoTracking()
            .Where(boxBaseFilter);

        if (locationId.HasValue)
        {
            var locId = locationId.Value;
            boxQuery = boxQuery.Where(b => b.LocationId == locId);
        }

        // Build item query with filters
        var itemQuery = dbContext.Items
            .AsNoTracking()
            .Where(itemBaseFilter);

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

        // Count total results at database level
        var boxCountTask = boxId.HasValue ? Task.FromResult(0) : boxQuery.CountAsync(cancellationToken);
        var itemCountTask = itemQuery.CountAsync(cancellationToken);
        await Task.WhenAll(boxCountTask, itemCountTask);
        
        var totalResults = await boxCountTask + await itemCountTask;

        // Build combined ranked query with proper ordering
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
                Rank = EF.Functions.ToTsVector("english", b.Name + " " + b.Code + " " + (b.Description ?? ""))
                    .Rank(EF.Functions.PlainToTsQuery("english", query))
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
                Rank = EF.Functions.ToTsVector("english", i.Name + " " + (i.Description ?? ""))
                    .Rank(EF.Functions.PlainToTsQuery("english", query))
            });

        // Combine queries and apply pagination at database level
        var skip = (pageNumber - 1) * pageSize;
        var combinedQuery = boxId.HasValue 
            ? itemResultsQuery 
            : boxResultsQuery.Concat(itemResultsQuery);

        var pagedResults = await combinedQuery
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

        logger.LogDebug("PostgreSQL FTS search completed: {TotalResults} results, {PagedResults} on page {PageNumber}",
            totalResults, pagedResults.Count, pageNumber);

        return new SearchResultsInternal(pagedResults, totalResults);
    }
}
