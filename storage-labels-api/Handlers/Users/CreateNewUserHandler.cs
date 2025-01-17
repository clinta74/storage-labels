using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Users;
public record CreateNewUser(string UserId, string FirstName, string LastName, string EmailAddress) : IRequest<Result<User>>;
public class CreateNewUserHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider) : IRequestHandler<CreateNewUser, Result<User>>
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
        
        var result = dbContext
            .Users
            .Add(new User(
                UserId: request.UserId,
                FirstName: request.FirstName,
                LastName: request.LastName,
                EmailAddress: request.EmailAddress,
                Created: timeProvider.GetUtcNow())
            );

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Entity);
    }
}