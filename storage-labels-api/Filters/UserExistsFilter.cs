using StorageLabelsApi.Extensions;
using StorageLabelsApi.Handlers.Users;

namespace StorageLabelsApi.Filters;

public class UserExistsEndpointFilter(IMediator mediator, ILogger<UserExistsEndpointFilter> logger) 
    : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var userId = context.HttpContext.TryGetUserId();

        if (!await mediator.Send(new UserExists(userId ?? string.Empty)))
        {
            logger.LogError("User id {userId} not found", userId);
            return Results.Problem("User not found.");
        }

        return await next(context);
    }
}