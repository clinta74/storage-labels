using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Users;
public record CreateNewUser(string UserId, string FirstName, string LastName, string EmailAddress) : IRequest<Result<User>>;
public class CreateNewUserHandler(StorageLabelsDbContext dbContext, TimeProvider timeProvider, ILogger<CreateNewUserHandler> logger) : IRequestHandler<CreateNewUser, Result<User>>
{
    public async Task<Result<User>> Handle(CreateNewUser request, CancellationToken cancellationToken)
    {
        // var userExists = await dbContext
        //     .Users
        //     .AsNoTracking()
        //     .Where(u => u.UserId == request.UserId)
        //     .AnyAsync(cancellationToken);

        // if (userExists)
        // {
        //     logger.LogError("User already exists: {user}", request);
        //     Result.Conflict("User already exists.");
        // }

        var result = dbContext
            .Users
            .Add(new User
            {
                UserId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                EmailAddress = request.EmailAddress,
                Created = timeProvider.GetUtcNow()
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Entity);
    }
}

