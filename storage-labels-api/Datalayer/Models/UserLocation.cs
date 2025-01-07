using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

[Table("UserLocations")]
public record UserLocation
{
    public required string UserId { get; init; }
    public long LocationId { get; init; }
    public required string Name { get; init; }
    public required AccessLevels AccessLevel  { get; init; }
    public required DateTimeOffset Created { get; init; }
    public required DateTimeOffset Updated { get; init; }
    public User User { get; } = null!;
    public Location Location { get; } = null!;
};
