using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Boxes")]
public record Box
{
    [Key]
    public required Guid BoxId { get; init; }
    public required string Code { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public required long LocationId { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
    public required DateTimeOffset Access { get; init; }
    public Location Location { get; } = null!;
    public ICollection<Item> Items { get; } = [];
}

