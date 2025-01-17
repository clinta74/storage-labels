using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("UserLocations")]
public record UserLocation(
    string UserId,
    long LocationId,
    AccessLevels AccessLevel,
    DateTimeOffset Created,
    DateTimeOffset Updated)
{
    public User User { get; } = null!;
    public Location Location { get; } = null!;
};
