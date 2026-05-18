using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Services.Authentication;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static async Task<IResult> GetCurrentUser(
        HttpContext context,
        [FromServices] IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var result = await authService.GetCurrentUserAsync(userId, cancellationToken);
        return result.ToMinimalApiResult();
    }
}
