using System.ComponentModel.DataAnnotations.Schema;
using NpgsqlTypes;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Items")]
public record Item(
    Guid ItemId,
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId,
    DateTimeOffset Created,
    DateTimeOffset Updated)
{
    public Box Box { get; } = null!;
    public ImageMetadata? ImageMetadata { get; set; }
    
    /// <summary>
    /// Full-text search vector generated from Name and Description.
    /// Managed automatically by PostgreSQL as a generated column.
    /// </summary>
    public NpgsqlTsVector? SearchVector { get; set; }
};