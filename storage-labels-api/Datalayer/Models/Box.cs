using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Boxes")]
public record Box(
    Guid BoxId,
    string Code,
    string Name,
    string? Description,
    string? ImageUrl,
    long LocationId,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    DateTimeOffset Access)
{
    public Location Location { get; } = null!;
    public ICollection<Item> Items { get; } = [];
}