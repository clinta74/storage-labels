using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Search;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Endpoints.Search;

internal static partial class SearchEndpoints
{
    private static async Task<Results<Ok<List<SearchResultResponse>>, ProblemHttpResult>> SearchBoxesAndItems(
        HttpContext context,
        [FromServices] ISearchService searchService,
        ILogger logger,
        [FromQuery] string query,
        [FromQuery] long? locationId = null,
        [FromQuery] Guid? boxId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var userId = context.GetUserId();
        var stopwatch = Stopwatch.StartNew();
        logger.SearchStarted(query, userId, locationId, boxId, pageNumber, pageSize);

        try
        {
            var response = await searchService.SearchBoxesAndItemsAsync(
                query, userId, locationId, boxId, pageNumber, pageSize, cancellationToken);

            stopwatch.Stop();
            logger.SearchCompleted(query, response.TotalResults, pageSize, stopwatch.ElapsedMilliseconds);

            if (response.TotalResults == 0)
                logger.SearchNoResults(query, userId);

            context.Response.Headers["x-total-count"] = response.TotalResults.ToString();

            var results = new List<SearchResultResponse>();
            await foreach (var r in response.Results)
                results.Add(new SearchResultResponse(r));

            return TypedResults.Ok(results);
        }
        catch (Exception ex)
        {
            logger.SearchFailed(ex, query, userId);
            return TypedResults.Problem("Search failed. Please try again.", statusCode: 500);
        }
    }
}
