using System.Text.Json.Serialization;
using BoxModel = StorageLabelsApi.DataLayer.Models.Box;

namespace StorageLabelsApi.Models.DTO.Box;

[method: JsonConstructor]
public record BoxResponse(
    Guid BoxId,
    string Code,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId,
    long LocationId,
    int ItemCount,
    DateTimeOffset Created,
    DateTimeOffset Updated,
    DateTimeOffset LastAccessed)
{
    public BoxResponse(BoxModel box, int itemCount) : this(
        box.BoxId,
        box.Code,
        box.Name,
        box.Description,
        box.ImageUrl,
        box.ImageMetadataId,
        box.LocationId,
        itemCount,
        box.Created,
        box.Updated,
        box.LastAccessed)
    { }
};
