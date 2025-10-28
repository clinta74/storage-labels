namespace StorageLabelsApi.Models.DTO;

public class SearchResultResponse
{
    public required string Type { get; set; } // "box" or "item"
    
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

public class SearchResultsResponse
{
    public List<SearchResultResponse> Results { get; set; } = new();
}
