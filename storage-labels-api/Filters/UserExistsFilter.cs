using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Filters;

public class UserExistsEndpointFilter(StorageLabelsDbContext dbContext, ILogger<UserExistsEndpointFilter> logger) 
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.TryGetUserId();

        var exists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(u => u.UserId == (userId ?? string.Empty));

        if (!exists)
        {
            logger.UserNotFound(userId ?? "null");
            return Results.Problem("User not found.");
        }

        return await next(context);
    }
}