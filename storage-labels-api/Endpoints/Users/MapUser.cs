using StorageLabelsApi.Filters;
using StorageLabelsApi.Models;

namespace StorageLabelsApi.Endpoints.Users;

internal partial class UserEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("user")
            .WithTags("Users");

        group.MapGet("/", GetCurrentUser)
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .WithName("Get Current User");

        group.MapGet("/{userid}", GetUserById)
            .RequireAuthorization(Policies.Read_User)
            .WithName("Get User By UserId");

        group.MapGet("exists", GetUserExists)
            .WithName("Get User Exists");

        group.MapPost("/", CreateUser)
            .WithName("Add User");

        group.MapGet("/preferences", GetUserPreferences)
            .WithName("Get User Preferences");

        group.MapPut("/preferences", UpdateUserPreferences)
            .WithName("Update User Preferences");

        group.MapGet("/export/{exportType}", ExportUserData)
            .WithName("Export User Data")
            .WithDescription("Export user data as CSV. Valid export types: locations, boxes, items");

        group.MapGet("/all", GetAllUsers)
            .RequireAuthorization(Policies.Read_User)
            .WithName("Get All Users");

        group.MapPut("/{userid}/role", UpdateUserRole)
            .RequireAuthorization(Policies.Write_User)
            .WithName("Update User Role");

        group.MapDelete("/{userId}", DeleteUser)
            .RequireAuthorization(Policies.Write_User)
            .WithName("Delete User");
    }
}
