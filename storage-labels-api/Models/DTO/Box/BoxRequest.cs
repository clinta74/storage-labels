using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO;

public record CreateBoxRequest(
    [Required] string Code, 
    [Required] string Name, 
    [Required] long LocationId,
    string? Description,
    string? ImageUrl
);
