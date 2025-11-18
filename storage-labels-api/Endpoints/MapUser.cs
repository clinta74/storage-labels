using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Users;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.User;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapUser(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("user")
            .WithTags("Users")
            .MapUsersEndpoints();
    }

    private static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/", GetCurrentUser)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .WithName("Get Current User");

        routeBuilder.MapGet("/{userid}", GetUserById)
            .RequireAuthorization(Policies.Read_User)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .WithName("Get User By UserId");

        routeBuilder.MapGet("exists", GetUserExists)
            .Produces<bool>(StatusCodes.Status200OK)
            .WithName("Get User Exists");


        routeBuilder.MapPost("/", CreateUser)
            .Produces<UserResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Add User");

        routeBuilder.MapGet("/preferences", GetUserPreferences)
            .Produces<UserPreferencesResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .WithName("Get User Preferences");

        routeBuilder.MapPut("/preferences", UpdateUserPreferences)
            .Produces<UserPreferencesResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status400BadRequest)
            .WithName("Update User Preferences");

        routeBuilder.MapGet("/all", GetAllUsers)
            .RequireAuthorization(Policies.Read_User)
            .Produces<IEnumerable<UserWithRoles>>(StatusCodes.Status200OK)
            .WithName("Get All Users");

        routeBuilder.MapPut("/{userid}/role", UpdateUserRole)
            .RequireAuthorization(Policies.Write_User)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update User Role");

        return routeBuilder;
    }

    private static async Task<IResult> GetCurrentUser(HttpContext context, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();

        var user = await mediator.Send(new GetUserById(userid), cancellationToken);
        return user
            .Map(user => new UserResponse(user))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> GetUserById(string userid, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var user = await mediator.Send(new GetUserById(userid), cancellationToken);
        return user
            .Map(user => new UserResponse(user))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> GetUserExists(HttpContext context, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.TryGetUserId();

        var userExists = userId is null ? false : await mediator.Send(new UserExists(userId), cancellationToken);
        return Results.Ok(userExists);
    }

    private static async Task<IResult> CreateUser(HttpContext context, CreateUserRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var email = context.User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value 
                    ?? context.User.FindFirst("email")?.Value 
                    ?? "unknown@localhost";
        
        var user = await mediator.Send(new CreateNewUser(userId, request.FirstName, request.LastName, email), cancellationToken);

        return user
            .Map(user => new UserResponse(user))
            .ToMinimalApiResult(); ;
    }

    private static async Task<IResult> GetUserPreferences(HttpContext context, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var preferences = await mediator.Send(new GetUserPreferences(userId), cancellationToken);
        
        return preferences.ToMinimalApiResult();
    }

    private static async Task<IResult> UpdateUserPreferences(
        HttpContext context, 
        UserPreferencesResponse request, 
        [FromServices] IMediator mediator, 
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var preferences = await mediator.Send(new Handlers.Users.UpdateUserPreferences(userId, request), cancellationToken);
        
        return preferences.ToMinimalApiResult();
    }

    private static async Task<IResult> GetAllUsers([FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetAllUsers(), cancellationToken);
        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> UpdateUserRole(
        string userid,
        UpdateUserRoleRequest request,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new Handlers.Users.UpdateUserRole(userid, request.Role), cancellationToken);
        return result.ToMinimalApiResult();
    }
}

public record UpdateUserRoleRequest(string Role);
}