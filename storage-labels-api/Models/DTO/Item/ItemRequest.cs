namespace StorageLabelsApi.Models.DTO;

public record ItemRequest(
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId
);
