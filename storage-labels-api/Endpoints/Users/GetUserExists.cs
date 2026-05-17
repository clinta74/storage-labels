using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;

namespace StorageLabelsApi.Endpoints.Users;

internal partial class UserEndpoints
{
    private static async Task<Ok<bool>> GetUserExists(HttpContext context, StorageLabelsDbContext dbContext, CancellationToken cancellationToken)
    {
        var userId = context.TryGetUserId();

        if (userId is null)
            return TypedResults.Ok(false);

        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserId == userId, cancellationToken);

        return TypedResults.Ok(exists);
    }
}
