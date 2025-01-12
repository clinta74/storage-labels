using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Handlers.Users;
public record UserExists(string UserId) : IRequest<bool>;
public class UserExistsHandler(StorageLabelsDbContext dbContext) : IRequestHandler<UserExists, bool>
{
    public async Task<bool> Handle(UserExists request, CancellationToken cancellationToken)
    {
        return await dbContext
            .Users
            .AsNoTracking()
            .AnyAsync(user => user.UserId == request.UserId, cancellationToken);
    }
}
