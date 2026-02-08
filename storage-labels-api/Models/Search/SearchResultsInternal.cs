namespace StorageLabelsApi.Models.Search;

/// <summary>
/// Internal search result model used by service layer
/// </summary>
public record SearchResult(
    string Type,
    float Rank,
    string? BoxId,
    string? BoxName,
    string? BoxCode,
    string? ItemId,
    string? ItemName,
    string? ItemCode,
    string LocationId,
    string LocationName);

/// <summary>
/// Internal response type for search service - contains results and metadata
/// </summary>
public record SearchResultsInternal(
    List<SearchResult> Results,
    int TotalResults);
