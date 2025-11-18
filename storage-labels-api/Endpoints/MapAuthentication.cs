using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Handlers.Authentication;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.Authentication;
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
        [FromBody] Login request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> Register(
        [FromBody] Register request,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(request, cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> Logout(
        HttpContext context,
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Results.Unauthorized();
        }

        var request = new Logout(userId);
        var result = await mediator.Send(request, cancellationToken);
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
}

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record AdminResetPasswordRequest(string UserId, string NewPassword);
