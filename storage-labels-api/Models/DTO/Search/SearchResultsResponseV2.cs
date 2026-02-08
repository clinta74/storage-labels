namespace StorageLabelsApi.Models.DTO.Search;

/// <summary>
/// Search result with relevance ranking (v2)
/// </summary>
public class SearchResultV2
{
    public required string Type { get; set; } // "box" or "item"
    
    /// <summary>
    /// Search relevance score (higher = more relevant)
    /// </summary>
    public float Rank { get; set; }
    
    // Box fields
    public string? BoxId { get; set; }
    public string? BoxName { get; set; }
    public string? BoxCode { get; set; }
    
    // Item fields
    public string? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? ItemCode { get; set; }
    
    // Common fields
    public required string LocationId { get; set; }
    public required string LocationName { get; set; }
}

/// <summary>
/// Paginated search results (v2)
/// </summary>
public class SearchResultsResponseV2
{
    /// <summary>
    /// Search results ordered by relevance
    /// </summary>
    public List<SearchResultV2> Results { get; set; } = [];

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Number of results per page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of results across all pages
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalResults / (double)PageSize);

    /// <summary>
    /// Whether there are more pages available
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Whether there are previous pages available
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
}
