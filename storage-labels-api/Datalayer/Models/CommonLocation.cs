using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("CommonLocations")]
public record CommonLocation
{
    [Key]
    public int CommonLocationId { get; init; }
    public required string Name { get; init; }
};
