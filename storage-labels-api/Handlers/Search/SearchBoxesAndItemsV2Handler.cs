using System.Diagnostics;
using Ardalis.Result;
using Mediator;
using StorageLabelsApi.Logging;
using StorageLabelsApi.Models.DTO.Search;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.Search;

/// <summary>
/// Search request for v2 API with pagination
/// </summary>
public record SearchBoxesAndItemsQueryV2(
    string Query, 
    string UserId, 
    long? LocationId, 
    Guid? BoxId,
    int PageNumber,
    int PageSize) : IRequest<Result<SearchResultsResponseV2>>;

/// <summary>
/// Handler for v2 search with pagination - delegates to ISearchService for database-specific implementation
/// </summary>
public class SearchBoxesAndItemsV2Handler(
    ISearchService searchService,
    ILogger<SearchBoxesAndItemsV2Handler> logger) 
    : IRequestHandler<SearchBoxesAndItemsQueryV2, Result<SearchResultsResponseV2>>
{
    public async ValueTask<Result<SearchResultsResponseV2>> Handle(
        SearchBoxesAndItemsQueryV2 request, 
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

            return Result<SearchResultsResponseV2>.Success(response);
        }
        catch (Exception ex)
        {
            logger.SearchFailed(ex, request.Query, request.UserId);
            return Result<SearchResultsResponseV2>.Error("Search failed. Please try again.");
        }
    }
}
