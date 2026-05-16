using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Locations;

public record LocationWithAccess(
    Location location,
    AccessLevels AccessLevel) : 
    Location(location.LocationId, location.Name, location.Created, location.Updated);
