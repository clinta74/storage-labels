using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Services.Authentication;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static async Task<IResult> Refresh(
        HttpContext context,
        [FromServices] IAuthenticationService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var refreshToken = context.Request.Cookies[refreshTokenOptions.Value.CookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Unauthorized();
        }

        var result = await authService.RefreshAsync(refreshToken, cancellationToken);

        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        else if (result.Status == ResultStatus.Unauthorized)
        {
            ClearRefreshTokenCookie(context, refreshTokenOptions.Value);
        }

        return result.ToMinimalApiResult();
    }
}
