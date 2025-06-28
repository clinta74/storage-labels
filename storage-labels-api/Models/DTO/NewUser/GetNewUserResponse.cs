using Auth0.ManagementApi.Models;

namespace StorageLabelsApi.Models.DTO.NewUser;

public record GetNewUserResponse(string FirstName, string LastName, string EmailAddress)
{
    public GetNewUserResponse(User user) : this(user.FirstName, user.LastName, user.Email) { }
}
