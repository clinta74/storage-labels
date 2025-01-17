namespace StorageLabelsApi.Models.DTO;

public record CreateItemResponse(
    Guid ItemId,
    Guid BoxId,
    string Name,
    string? Description,
    string? ImageUrl,
    DateTimeOffset Created,
    DateTimeOffset Updated);
