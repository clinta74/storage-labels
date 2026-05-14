using Ardalis.Result.AspNetCore;
using Ardalis.Result.FluentValidation;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using StorageLabelsApi.Extensions;
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
        [FromBody] LoginRequest request,
        [FromServices] IAuthenticationService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var validation = await new LoginValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors()).ToMinimalApiResult();
        }

        var result = await authService.LoginAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> Register(
        HttpContext context,
        [FromBody] RegisterRequest request,
        [FromServices] IAuthenticationService authService,
        IOptions<RefreshTokenSettings> refreshTokenOptions,
        CancellationToken cancellationToken)
    {
        var validation = await new RegisterValidator().ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result<AuthenticationResult>.Invalid(validation.AsErrors()).ToMinimalApiResult();
        }

        var result = await authService.RegisterAsync(request, cancellationToken);
        if (result.IsSuccess)
        {
            IssueRefreshTokenCookie(context, refreshTokenOptions.Value, result.Value);
        }
        return result.ToMinimalApiResult();
    }

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
        else if (result.Status == Ardalis.Result.ResultStatus.Unauthorized)
        {
            ClearRefreshTokenCookie(context, refreshTokenOptions.Value);
        }

        return result.ToMinimalApiResult();
    }

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

    private static IResult GetAuthConfig(
        IOptions<AuthenticationSettings> authSettings)
    {
        var s = authSettings.Value;
        var config = new AuthConfigResponse(
            s.Mode,
            s.Local.AllowRegistration,
            s.Local.RequireEmailConfirmation
        );
        return Result.Success(config).ToMinimalApiResult();
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
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> AdminResetPassword(
        [FromBody] AdminResetPasswordRequest request,
        [FromServices] IAuthenticationService authService,
        CancellationToken cancellationToken)
    {
        var result = await authService.AdminResetPasswordAsync(request.UserId, request.NewPassword, cancellationToken);
        return result.ToMinimalApiResult();
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

    private sealed class LoginValidator : AbstractValidator<LoginRequest>
    {
        public LoginValidator()
        {
            RuleFor(x => x.UsernameOrEmail).NotEmpty().WithMessage("Username or email is required");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
        }
    }

    private sealed class RegisterValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterValidator()
        {
            RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required").EmailAddress().WithMessage("Valid email address is required");
            RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required").MinimumLength(3).WithMessage("Username must be at least 3 characters");
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required").MinimumLength(8).WithMessage("Password must be at least 8 characters");
            RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required").MinimumLength(2).WithMessage("First name must be at least 2 characters");
            RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required").MinimumLength(2).WithMessage("Last name must be at least 2 characters");
        }
    }
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminResetPasswordRequest(string UserId, string NewPassword);

