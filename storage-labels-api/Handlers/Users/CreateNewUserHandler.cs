using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Users;

/// <summary>
/// Creates a new user record - typically called during registration.
/// This handler is now primarily for admin-created users.
/// </summary>
public record CreateNewUser(string UserId, string FirstName, string LastName, string EmailAddress) : IRequest<Result<User>>;

public class CreateNewUserHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<CreateNewUser, Result<User>>
{
    public async ValueTask<Result<User>> Handle(CreateNewUser request, CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserId == request.UserId)
            .AnyAsync(cancellationToken);

        if (existingUser)
        {
            return Result.Conflict([$"User with the Id {request.UserId} already exists."]);
        }
        
        var user = new User(
            UserId: request.UserId,
            FirstName: request.FirstName,
            LastName: request.LastName,
            EmailAddress: request.EmailAddress,
            Created: timeProvider.GetUtcNow()
        );

        var result = dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Entity);
    }
}