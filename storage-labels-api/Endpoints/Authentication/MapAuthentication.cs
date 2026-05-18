using Microsoft.AspNetCore.Http;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.Authentication;

namespace StorageLabelsApi.Endpoints.Authentication;

internal partial class AuthenticationEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("auth")
            .WithTags("Authentication");

        // Public endpoints
        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithName("Login")
            .WithDescription("Authenticate user and get JWT token")
            .Produces<AuthenticationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", Register)
            .AllowAnonymous()
            .WithName("Register")
            .WithDescription("Register new user account")
            .Produces<AuthenticationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/config", GetAuthConfig)
            .AllowAnonymous()
            .WithName("GetAuthConfig")
            .WithDescription("Get authentication configuration")
            .Produces<AuthConfigResponse>(StatusCodes.Status200OK);

        group.MapPost("/refresh", Refresh)
            .AllowAnonymous()
            .WithName("RefreshToken")
            .WithDescription("Refresh access token using refresh cookie")
            .Produces<AuthenticationResult>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        // Protected endpoints
        group.MapPost("/logout", Logout)
            .WithName("Logout")
            .WithDescription("Logout current user")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/me", GetCurrentUser)
            .WithName("GetCurrentUser")
            .WithDescription("Get current authenticated user information")
            .Produces<UserInfoResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/change-password", ChangePassword)
            .WithName("ChangePassword")
            .WithDescription("Change the current user's password")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapPost("/admin/reset-password", AdminResetPassword)
            .RequireAuthorization(Policies.Write_User)
            .WithName("AdminResetPassword")
            .WithDescription("Admin endpoint to reset any user's password")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }
}
