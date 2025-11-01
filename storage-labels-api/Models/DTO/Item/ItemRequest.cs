namespace StorageLabelsApi.Models.DTO.Item;

public record ItemRequest(
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId
);
