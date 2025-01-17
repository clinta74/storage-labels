using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO;

public record LocationRequest(
    [Required] string Name
);

