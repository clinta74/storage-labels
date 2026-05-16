using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Handlers.Locations;

namespace StorageLabelsApi.Models.DTO.Location;

public record LocationResponse(
    long LocationId,
    string Name,
    AccessLevels AccessLevel,
    DateTimeOffset Created,
    DateTimeOffset Updated
)
{
    public LocationResponse(LocationWithAccess location) : this(
        location.LocationId,
        location.Name,
        location.AccessLevel,
        location.Created,
        location.Updated)
    { }
}