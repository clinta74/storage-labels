using System.ComponentModel.DataAnnotations.Schema;

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
};