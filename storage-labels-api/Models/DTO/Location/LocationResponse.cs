using StorageLabelsApi.DataLayer.Models;
using LocationModel = StorageLabelsApi.DataLayer.Models.Location;

namespace StorageLabelsApi.Models.DTO.Location;

public record LocationResponse(
    long LocationId,
    string Name,
    AccessLevels AccessLevel,
    DateTimeOffset Created,
    DateTimeOffset Updated
)
{
    public LocationResponse(LocationModel location, AccessLevels accessLevel) : this(
        location.LocationId,
        location.Name,
        accessLevel,
        location.Created,
        location.Updated)
    { }
}