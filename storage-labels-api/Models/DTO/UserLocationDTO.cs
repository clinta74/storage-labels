using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO;

public record UserLocationResponse(
    string UserId,
    string FirstName,
    string LastName,
    string EmailAddress,
    AccessLevels AccessLevel
)
{
    public UserLocationResponse(UserLocation userLocation) : this(
        userLocation.UserId,
        userLocation.User.FirstName,
        userLocation.User.LastName,
        userLocation.User.EmailAddress,
        userLocation.AccessLevel
    ) { }
}

public record AddUserLocationRequest(
    string EmailAddress,
    AccessLevels AccessLevel
);

public record UpdateUserLocationRequest(
    AccessLevels AccessLevel
);
