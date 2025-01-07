using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Items")]
public record Item
{
    public required Guid ItemId { get; init; }
    public required Guid BoxId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
    public Box Box { get; } = null!;
};
