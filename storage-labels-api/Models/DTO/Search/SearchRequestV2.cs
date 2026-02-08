using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO.Search;

/// <summary>
/// Search request with pagination support (v2)
/// </summary>
public record SearchRequestV2
{
    /// <summary>
    /// Search query string
    /// </summary>
    [Required]
    [MinLength(1)]
    public required string Query { get; init; }

    /// <summary>
    /// Optional location ID filter
    /// </summary>
    public long? LocationId { get; init; }

    /// <summary>
    /// Optional box ID filter (for searching items within a box)
    /// </summary>
    public Guid? BoxId { get; init; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Page size (max 100)
    /// </summary>
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
