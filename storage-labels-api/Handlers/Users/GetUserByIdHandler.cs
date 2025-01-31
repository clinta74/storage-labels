using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Handlers.Users;

public record GetUserById(string UserId) : IRequest<Result<User>>;

public class GetUserByIdHandler(StorageLabelsDbContext dbContext) : IRequestHandler<GetUserById, Result<User>>
{

    public async Task<Result<User>> Handle(GetUserById request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserId == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Result.NotFound($"User Id ({request.UserId}) not found.");
        }

        return Result.Success(user);
    }
}