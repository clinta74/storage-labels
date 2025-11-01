using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO.Location;

public record LocationRequest(
    [Required] string Name
);

