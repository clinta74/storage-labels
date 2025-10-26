using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO;

public record ItemResponse(
    Guid ItemId,
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId,
    DateTimeOffset Created,
    DateTimeOffset Updated)
{
    public ItemResponse(Item item) : this(
        item.ItemId,
        item.BoxId,
        item.Name,
        item.Description,
        item.ImageUrl,
        item.ImageMetadataId,
        item.Created,
        item.Updated)
    { }
};
