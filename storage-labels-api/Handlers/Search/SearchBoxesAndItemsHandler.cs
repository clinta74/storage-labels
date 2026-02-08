using System.Diagnostics;
using Ardalis.Result;
using Mediator;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.Search;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.Search;

/// <summary>
/// Search request with pagination
/// </summary>
public record SearchBoxesAndItemsQuery(
    string Query, 
    string UserId, 
    long? LocationId, 
    Guid? BoxId,
    int PageNumber,
    int PageSize) : IRequest<Result<SearchResultsInternal>>;

/// <summary>
/// Handler for search with pagination - delegates to ISearchService for database-specific implementation
/// </summary>
public class SearchBoxesAndItemsHandler(
    ISearchService searchService,
    ILogger<SearchBoxesAndItemsHandler> logger) 
    : IRequestHandler<SearchBoxesAndItemsQuery, Result<SearchResultsInternal>>
{
    public async ValueTask<Result<SearchResultsInternal>> Handle(
        SearchBoxesAndItemsQuery request, 
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.SearchStarted(request.Query, request.UserId, request.LocationId, request.BoxId, 
            request.PageNumber, request.PageSize);

        try
        {
            var response = await searchService.SearchBoxesAndItemsAsync(
                request.Query,
                request.UserId,
                request.LocationId,
                request.BoxId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            stopwatch.Stop();
            logger.SearchCompleted(request.Query, response.TotalResults, response.Results.Count, stopwatch.ElapsedMilliseconds);

            if (response.TotalResults == 0)
            {
                logger.SearchNoResults(request.Query, request.UserId);
            }

            return Result<SearchResultsInternal>.Success(response);
        }
        catch (Exception ex)
        {
            logger.SearchFailed(ex, request.Query, request.UserId);
            return Result<SearchResultsInternal>.Error("Search failed. Please try again.");
        }
    }
}
