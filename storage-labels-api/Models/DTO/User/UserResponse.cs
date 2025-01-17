using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO;

public record UserResponse(string UserId, string FirstName, string LastName, string EmailAddress, DateTimeOffset created)
{
    public UserResponse(User user) : this(user.UserId, user.FirstName, user.LastName, user.EmailAddress, user.Created) { }
}
