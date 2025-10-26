using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO;

public record BoxResponse(
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
    public BoxResponse(Box box) : this(
        box.BoxId,
        box.Code,
        box.Name,
        box.Description,
        box.ImageUrl,
        box.ImageMetadataId,
        box.LocationId,
        box.Created,
        box.Updated,
        box.LastAccessed)
    { }
};
