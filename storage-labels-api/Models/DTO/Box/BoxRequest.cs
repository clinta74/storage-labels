using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO.Box;

public record BoxRequest(
    [Required] string Code, 
    [Required] string Name, 
    [Required] long LocationId,
    string? Description,
    string? ImageUrl,
    Guid? ImageMetadataId
);
