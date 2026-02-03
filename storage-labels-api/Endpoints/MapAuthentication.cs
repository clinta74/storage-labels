using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Handlers.Authentication;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.Authentication;
using StorageLabelsApi.Models.Settings;
using StorageLabelsApi.Services.Authentication;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

public static class MapAuthentication
{
    public static void MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
    {
        var auth = app.MapGroup("/api/auth").WithTags("Authentication");

        // Public endpoints
        auth.MapPost("/login", Login)
            .WithName("Login")
            .WithDescription("Authenticate user and get JWT token")
            .Produces<AuthenticationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/register", Register)
            .WithName("Register")
            .WithDescription("Register new user account")
            .Produces<AuthenticationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        auth.MapGet("/config", GetAuthConfig)
            .WithName("GetAuthConfig")
            .WithDescription("Get authentication configuration")
            .Produces<AuthConfigResponse>(StatusCodes.Status200OK);

        auth.MapPost("/refresh", Refresh)
            .WithName("RefreshToken")
            .WithDescription("Refresh access token using refresh cookie")
            .Produces<AuthenticationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Protected endpoints
        auth.MapPost("/logout", Logout)
            .RequireAuthorization()
            .WithName("Logout")
            .WithDescription("Logout current user")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapGet("/me", GetCurrentUser)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .WithDescription("Get current authenticated user information")
            .Produces<UserInfoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/change-password", ChangePassword)
            .RequireAuthorization()
            .WithName("ChangePassword")
            .WithDescription("Change the current user's password")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        auth.MapPost("/admin/reset-password", AdminResetPassword)
            .RequireAuthorization(Policies.Write_User)
            .WithName("AdminResetPassword")
            .WithDescription("Admin endpoint to reset any user's password")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> Login(
        HttpContext context,
        [FromBody] Login request,
        IMediator mediator,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> Register(
        HttpContext context,
        [FromBody] Register request,
        IMediator mediator,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> Logout(
        HttpContext context,
        IMediator mediator,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var request = new Logout(userId);
        var result = await mediator.Send(request, cancellationToken);
        if (result.IsSuccess)
        {
            ClearRefreshTokenCookie(context, refreshTokenOptions.Value);
        }
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> Refresh(
        HttpContext context,
        IMediator mediator,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var refreshToken = context.Request.Cookies[refreshTokenOptions.Value.CookieName];
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Results.Unauthorized();
        }

        var request = new RefreshAuthentication(refreshToken);
        var result = await mediator.Send(request, cancellationToken);

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

    private static async Task<IResult> GetCurrentUser(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var request = new GetCurrentUserInfo(userId);
        var result = await mediator.Send(request, cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> GetAuthConfig(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var request = new GetAuthConfig();
        var result = await mediator.Send(request, cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> ChangePassword(
        HttpContext context,
        [FromBody] ChangePasswordRequest request,
        [FromServices] IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var result = await authService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, cancellationToken);
        return result.IsSuccess 
            ? Results.Ok() 
            : Results.BadRequest(new { error = result.Errors });
    }

    private static async Task<IResult> AdminResetPassword(
        [FromBody] AdminResetPasswordRequest request,
        [FromServices] IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.AdminResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        return result.IsSuccess 
            ? Results.Ok() 
            : Results.BadRequest(new { error = result.Errors });
    }

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

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminResetPasswordRequest(string UserId, string NewPassword);
