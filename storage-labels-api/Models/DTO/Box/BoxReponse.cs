using BoxModel = StorageLabelsApi.DataLayer.Models.Box;

namespace StorageLabelsApi.Models.DTO.Box;

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
    public BoxResponse(BoxModel box) : this(
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
