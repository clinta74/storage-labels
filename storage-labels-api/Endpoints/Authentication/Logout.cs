using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Services.Authentication;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static async Task<IResult> Logout(
        HttpContext context,
        [FromServices] IAuthenticationService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var result = await authService.LogoutAsync(userId, cancellationToken);
        if (result.IsSuccess)
        {
            ClearRefreshTokenCookie(context, refreshTokenOptions.Value);
        }
        return result.ToMinimalApiResult();
    }
}
