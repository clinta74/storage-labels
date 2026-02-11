namespace StorageLabelsApi.Models.DTO.Search;

using StorageLabelsApi.Models.Search;

/// <summary>
/// Paginated search response DTO
/// </summary>
public record SearchResponse(
    IAsyncEnumerable<SearchResultResponse> Results,
    int TotalResults)
{
    public SearchResponse(SearchResultsInternal searchResults, IAsyncEnumerable<SearchResultResponse> results) 
        : this(results, searchResults.TotalResults)
    { }
}
