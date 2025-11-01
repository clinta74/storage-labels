using Auth0User = Auth0.ManagementApi.Models.User;

namespace StorageLabelsApi.Models.DTO.NewUser;

public record GetNewUserResponse(string FirstName, string LastName, string EmailAddress)
{
    public GetNewUserResponse(Auth0User user) : this(user.FirstName, user.LastName, user.Email) { }
}
