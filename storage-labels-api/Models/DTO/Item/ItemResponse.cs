using ItemModel = StorageLabelsApi.DataLayer.Models.Item;

namespace StorageLabelsApi.Models.DTO.Item;

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
    public ItemResponse(ItemModel item) : this(
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
