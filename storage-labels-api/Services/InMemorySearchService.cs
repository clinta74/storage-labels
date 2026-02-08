using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Models.Search;

namespace StorageLabelsApi.Services;

/// <summary>
/// Simple string-matching search implementation for testing and non-PostgreSQL databases
/// Uses LIKE-based searching instead of full-text search
/// </summary>
public class InMemorySearchService(
    StorageLabelsDbContext dbContext,
    ILogger<InMemorySearchService> logger) : ISearchService
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
        logger.LogDebug("In-memory search: {Query} for user {UserId}", query, userId);

        var searchTerm = query.ToLowerInvariant();

        // Build base query for user's accessible locations
        var accessibleLocationIds = await dbContext.UserLocations
            .AsNoTracking()
            .Where(ul => ul.UserId == userId && ul.AccessLevel != AccessLevels.None)
            .Select(ul => ul.LocationId)
            .ToListAsync(cancellationToken);

        if (!accessibleLocationIds.Any())
        {
            logger.LogDebug("User {UserId} has no accessible locations", userId);
            return new SearchResultsInternal(new List<SearchResult>(), 0);
        }

        // Combined results list
        var allResults = new List<SearchResult>();

        // Search boxes if not filtering by BoxId
        if (!boxId.HasValue)
        {
            var boxQuery = dbContext.Boxes
                .AsNoTracking()
                .Include(b => b.Location)
                .Where(b => accessibleLocationIds.Contains(b.LocationId));

            if (locationId.HasValue)
            {
                boxQuery = boxQuery.Where(b => b.LocationId == locationId.Value);
            }

            var boxes = await boxQuery.ToListAsync(cancellationToken);

            var boxResults = boxes
                .Where(b =>
                    (b.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.Code?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (b.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .Select(b => new SearchResult(
                    "box",
                    CalculateSimpleRank(searchTerm, b.Name, b.Code, b.Description),
                    b.BoxId.ToString(),
                    b.Name,
                    b.Code,
                    null,
                    null,
                    null,
                    b.LocationId.ToString(),
                    b.Location?.Name ?? "Unknown"))
                .ToList();

            allResults.AddRange(boxResults);
        }

        // Search items
        var itemQuery = dbContext.Items
            .AsNoTracking()
            .Include(i => i.Box)
                .ThenInclude(b => b.Location)
            .Where(i => accessibleLocationIds.Contains(i.Box.LocationId));

        if (locationId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.Box.LocationId == locationId.Value);
        }

        if (boxId.HasValue)
        {
            itemQuery = itemQuery.Where(i => i.BoxId == boxId.Value);
        }

        var items = await itemQuery.ToListAsync(cancellationToken);

        var itemResults = items
            .Where(i =>
                (i.Name?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.Description?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
            .Select(i => new SearchResult(
                "item",
                CalculateSimpleRank(searchTerm, i.Name, null, i.Description),
                i.BoxId.ToString(),
                i.Box.Name,
                i.Box.Code,
                i.ItemId.ToString(),
                i.Name,
                null,
                i.Box.LocationId.ToString(),
                i.Box.Location?.Name ?? "Unknown"))
            .ToList();

        allResults.AddRange(itemResults);

        // Sort by rank (descending)
        allResults = allResults
            .OrderByDescending(r => r.Rank)
            .ThenBy(r => r.Type)
            .ThenBy(r => r.BoxName)
            .ToList();

        var totalResults = allResults.Count;

        // Apply pagination
        var skip = (pageNumber - 1) * pageSize;
        var pagedResults = allResults
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        logger.LogDebug("In-memory search completed: {TotalResults} results, {PagedResults} on page {PageNumber}",
            totalResults, pagedResults.Count, pageNumber);

        return new SearchResultsInternal(pagedResults, totalResults);
    }

    /// <summary>
    /// Calculate a simple relevance rank based on where the search term appears
    /// </summary>
    private static float CalculateSimpleRank(string searchTerm, string? name, string? code, string? description)
    {
        float rank = 0f;

        // Exact match in name gets highest score
        if (name != null && name.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            rank += 10f;
        }
        // Starts with search term
        else if (name != null && name.StartsWith(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            rank += 5f;
        }
        // Contains search term
        else if (name != null && name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            rank += 2f;
        }

        // Code matches
        if (code != null)
        {
            if (code.Equals(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                rank += 8f;
            }
            else if (code.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            {
                rank += 3f;
            }
        }

        // Description matches (lower priority)
        if (description != null && description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
        {
            rank += 1f;
        }

        // Ensure non-zero rank for any match
        return rank > 0 ? rank : 0.1f;
    }
}
