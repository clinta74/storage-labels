using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Models.Settings;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints
{
    private static void IssueRefreshTokenCookie(HttpContext context, RefreshTokenSettings settings, AuthenticationResult result)
    {
        if (string.IsNullOrEmpty(result.RefreshToken) || result.RefreshTokenExpiresAt is null)
        {
            return;
        }

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = new DateTimeOffset(result.RefreshTokenExpiresAt.Value),
            Path = "/api/auth"
        };

        context.Response.Cookies.Append(settings.CookieName, result.RefreshToken, cookieOptions);
    }

    private static void ClearRefreshTokenCookie(HttpContext context, RefreshTokenSettings settings)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UnixEpoch,
            Path = "/api/auth"
        };

        context.Response.Cookies.Append(settings.CookieName, string.Empty, cookieOptions);
    }
}
