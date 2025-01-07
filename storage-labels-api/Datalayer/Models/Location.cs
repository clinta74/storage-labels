using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Locations")]
public record Location
{
    [Key]
    public long LocationId { get; init; }
    public required string Name { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
    public ICollection<UserLocation> UserLocations { get; } = [];
    public ICollection<Box> Boxes { get; } = [];
};
