using Auth0.ManagementApi.Models;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.NewUsers;

public record GetNewUser(string userId) : IRequest<Result<User>>;
public class GetNewUserHandler(IAuth0ManagementApiClient auth0ManagementApiClient) : IRequestHandler<GetNewUser, Result<User>>
{
    public async Task<Result<User>> Handle(GetNewUser request, CancellationToken cancellationToken)
    {
        if (auth0ManagementApiClient.Client is null)
        {
            return Result.CriticalError("Auth0ManagementApiClient not initialized.");
        }

        var userData = await auth0ManagementApiClient.Client.Users.GetAsync(request.userId, null, true, cancellationToken);

        if (userData is null)
        {
            return Result.NotFound("User not found by user management service.");
        }

        return Result.Success(userData);
    }

}
