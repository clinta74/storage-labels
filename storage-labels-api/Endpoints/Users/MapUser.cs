using StorageLabelsApi.Filters;
using StorageLabelsApi.Models;

namespace StorageLabelsApi.Endpoints.Users;

internal static partial class UserEndpoints
{
    internal static IEndpointRouteBuilder MapUser(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("user")
            .WithTags("Users")
            .MapUsersEndpoints();
    }

    private static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/", GetCurrentUser)
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .WithName("Get Current User");

        routeBuilder.MapGet("/{userid}", GetUserById)
            .RequireAuthorization(Policies.Read_User)
            .WithName("Get User By UserId");

        routeBuilder.MapGet("exists", GetUserExists)
            .WithName("Get User Exists");

        routeBuilder.MapPost("/", CreateUser)
            .WithName("Add User");

        routeBuilder.MapGet("/preferences", GetUserPreferences)
            .WithName("Get User Preferences");

        routeBuilder.MapPut("/preferences", UpdateUserPreferences)
            .WithName("Update User Preferences");

        routeBuilder.MapGet("/export/{exportType}", ExportUserData)
            .WithName("Export User Data")
            .WithDescription("Export user data as CSV. Valid export types: locations, boxes, items");

        routeBuilder.MapGet("/all", GetAllUsers)
            .RequireAuthorization(Policies.Read_User)
            .WithName("Get All Users");

        routeBuilder.MapPut("/{userid}/role", UpdateUserRole)
            .RequireAuthorization(Policies.Write_User)
            .WithName("Update User Role");

        routeBuilder.MapDelete("/{userId}", DeleteUser)
            .RequireAuthorization(Policies.Write_User)
            .WithName("Delete User");

        return routeBuilder;
    }
}
