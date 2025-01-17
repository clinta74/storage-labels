using System.Runtime.CompilerServices;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Users;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO;
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
        var user = await mediator.Send(new CreateNewUser(userId, request.FirstName, request.LastName, request.EmailAddress), cancellationToken);

        return user
            .Map(user => new UserResponse(user))
            .ToMinimalApiResult(); ;
    }
}