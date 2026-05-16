using StorageLabelsApi.Handlers.Users;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Filters;

public class UserExistsEndpointFilter(IMediator mediator, ILogger<UserExistsEndpointFilter> logger) 
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.TryGetUserId();

        if (!await mediator.Send(new UserExists(userId ?? string.Empty)))
        {
            logger.UserNotFound(userId ?? "null");
            return Results.Problem("User not found.");
        }

        return await next(context);
    }
}