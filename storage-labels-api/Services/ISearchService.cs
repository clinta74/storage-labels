using StorageLabelsApi.Models.DTO.Search;

namespace StorageLabelsApi.Services;

/// <summary>
/// Service for searching boxes and items across different database providers
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Search for boxes and items with pagination
    /// </summary>
    Task<SearchResultsResponseV2> SearchBoxesAndItemsAsync(
        string query,
        string userId,
        long? locationId,
        Guid? boxId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
