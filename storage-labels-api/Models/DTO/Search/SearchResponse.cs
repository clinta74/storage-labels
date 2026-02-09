namespace StorageLabelsApi.Models.DTO.Search;

using StorageLabelsApi.Models.Search;

/// <summary>
/// Paginated search response DTO
/// </summary>
public record SearchResponse(
    IEnumerable<SearchResultResponse> Results,
    int TotalResults)
{
    public SearchResponse(SearchResultsInternal searchResults, IEnumerable<SearchResultResponse> results) 
        : this(results, searchResults.TotalResults)
    { }
}
