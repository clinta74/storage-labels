namespace StorageLabelsApi.Models.DTO;

public record CreateItemRequest(
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl
);
