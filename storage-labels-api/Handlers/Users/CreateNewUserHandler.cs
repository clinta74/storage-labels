using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Handlers.Users;
public record CreateNewUser(string UserId, string FirstName, string LastName) : IRequest<Result<User>>;
public class CreateNewUserHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider, IAuth0ManagementApiClient auth0ManagementApiClient) : IRequestHandler<CreateNewUser, Result<User>>
{
    public async Task<Result<User>> Handle(CreateNewUser request, CancellationToken cancellationToken)
    {
        var hasBoxCode = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserId == request.UserId)
            .AnyAsync(cancellationToken);

        if (hasBoxCode)
        {
            return Result.Conflict([$"User with the Id {request.UserId} already exists."]);
        }

        // Get email from Auth0
        if (auth0ManagementApiClient.Client is null)
        {
            return Result.CriticalError("Auth0ManagementApiClient not initialized.");
        }

        var auth0User = await auth0ManagementApiClient.Client.Users.GetAsync(request.UserId, null, true, cancellationToken);

        if (auth0User is null || string.IsNullOrEmpty(auth0User.Email))
        {
            return Result.NotFound("User email not found in Auth0.");
        }
        
        var result = dbContext
            .Users
            .Add(new User(
                UserId: request.UserId,
                FirstName: request.FirstName,
                LastName: request.LastName,
                EmailAddress: auth0User.Email,
                Created: timeProvider.GetUtcNow())
            );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Entity);
    }
}