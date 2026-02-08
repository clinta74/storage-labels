using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Boxes")]
public record Box(
    Guid BoxId,
    string Code,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId,
    long LocationId,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    DateTimeOffset LastAccessed)
{
    public Location Location { get; } = null!;
    public ICollection<Item> Items { get; } = [];
    public ImageMetadata? ImageMetadata { get; set; }
    
    /// <summary>
    /// Full-text search vector generated from Name, Code, and Description.
    /// Managed automatically by PostgreSQL as a generated column.
    /// </summary>
    public NpgsqlTsVector? SearchVector { get; set; }
}