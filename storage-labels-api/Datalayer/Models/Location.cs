using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("Locations")]
public record Location(
    long LocationId,
    string Name,
    DateTimeOffset Created,
    DateTimeOffset Updated)
{
    public ICollection<UserLocation> UserLocations { get; } = [];
    public ICollection<Box> Boxes { get; } = [];
}
