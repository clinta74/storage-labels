using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO;

public record BoxRequest(
    [Required] string Code, 
    [Required] string Name, 
    [Required] long LocationId,
    string? Description,
    string? ImageUrl
);
