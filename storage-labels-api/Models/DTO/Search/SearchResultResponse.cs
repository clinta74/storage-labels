namespace StorageLabelsApi.Models.DTO.Search;

using System.Text.Json.Serialization;
using StorageLabelsApi.Models.Search;

/// <summary>
/// Search result with relevance ranking (v2) - DTO for API response
/// </summary>
[method: JsonConstructor]
public record SearchResultResponse(
    string Type,
    float Rank,
    string? BoxId,
    string? BoxName,
    string? BoxCode,
    string? ItemId,
    string? ItemName,
    string? ItemCode,
    string LocationId,
    string LocationName)
{
    public SearchResultResponse(SearchResult result) : this(
        result.Type,
        result.Rank,
        result.BoxId,
        result.BoxName,
        result.BoxCode,
        result.ItemId,
        result.ItemName,
        result.ItemCode,
        result.LocationId,
        result.LocationName)
    { }
};
