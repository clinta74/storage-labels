using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Handlers.Users;

namespace StorageLabelsApi.Filters;

public class UserExistsFilter : ActionFilterAttribute
{
    private readonly IMediator _mediator;
    public UserExistsFilter(IMediator mediator)
    {
        _mediator = mediator;
    }
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var userId = context.HttpContext.GetUserId();

        if (userId is null && !await _mediator.Send(new UserExists(userId)))
        {
            context.Result = new NotFoundObjectResult($"User not found.");
            return;
        }

        await base.OnActionExecutionAsync(context, next);
    }
}