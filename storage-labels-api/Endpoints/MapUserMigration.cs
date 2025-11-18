using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Models;
using StorageLabelsApi.Services;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapUserMigration(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("migration")
            .WithTags("User Migration")
            .MapUserMigrationEndpoints();
    }

    private static IEndpointRouteBuilder MapUserMigrationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/unmigrated-users", GetUnmigratedUsers)
            .RequireAuthorization(Policies.Read_User)
            .Produces<List<UnmigratedUser>>(StatusCodes.Status200OK)
            .WithName("Get Unmigrated Users")
            .WithDescription("Get list of users from legacy system that haven't been migrated yet.");

        routeBuilder.MapPost("/migrate-user", MigrateUser)
            .RequireAuthorization(Policies.Write_User)
            .Produces<MigratedUserInfo>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Migrate Single User")
            .WithDescription("Migrate a single user with admin-specified username and password. " +
                           "Optionally require password change on first login.");

        return routeBuilder;
    }

    private static async Task<IResult> GetUnmigratedUsers(
        [FromServices] UserMigrationService migrationService,
        CancellationToken cancellationToken)
    {
        var users = await migrationService.GetUnmigratedUsersAsync(cancellationToken);
        return Results.Ok(users);
    }

    private static async Task<IResult> MigrateUser(
        [FromBody] MigrateUserRequest request,
        [FromServices] UserMigrationService migrationService,
        CancellationToken cancellationToken)
    {
        var result = await migrationService.MigrateUserAsync(
            request.UserId,
            request.Username,
            request.Password,
            request.RequirePasswordChange,
            cancellationToken);

        return result.IsSuccess 
            ? Results.Ok(result.Value) 
            : Results.BadRequest(new { error = result.Errors });
    }
}

public record MigrateUserRequest(
    string UserId,
    string Username,
    string Password,
    bool RequirePasswordChange = false
);
