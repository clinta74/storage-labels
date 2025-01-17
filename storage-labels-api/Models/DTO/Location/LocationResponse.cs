using StorageLabelsApi.DataLayer.Models;
namespace StorageLabelsApi.Models.DTO;

public record LocationResponse(
    long LocationId,
    string Name,
    DateTimeOffset Created,
    DateTimeOffset Updated
)
{
    public LocationResponse(Location location) : this(
        location.LocationId,
        location.Name,
        location.Created,
        location.Updated)
    { }
}