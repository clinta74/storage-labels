using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Handlers.Users;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapUser(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("user")
            .MapUsersEndpoints();
    }

    private static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/",
        async (
            HttpContext context,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var userid = context.GetUserId();
                if (userid is null)
                {
                    return Results.NotFound("Current user could not be found.");
                }

                var user = await mediator.Send(new GetUserById(userid), cancellationToken);
                return user
                    .Map(user => new GetUserByIdResponse(
                        user.UserId,
                        user.FirstName,
                        user.LastName,
                        user.EmailAddress,
                        user.Created)
                    )
                    .ToMinimalApiResult();
            })
            .Produces<GetUserByIdResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .WithName("Get Current User");

        routeBuilder.MapGet("/{userid}",
        async (
            string userid,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var user = await mediator.Send(new GetUserById(userid), cancellationToken);
                return user
                    .Map(user => new GetUserByIdResponse(
                        user.UserId,
                        user.FirstName,
                        user.LastName,
                        user.EmailAddress,
                        user.Created)
                    )
                    .ToMinimalApiResult();
            })
            .RequireAuthorization(Policies.Read_User)
            .Produces<GetUserByIdResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .WithName("Get User By UserId");

        routeBuilder.MapGet("exists", async (
            HttpContext context,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var userid = context.GetUserId();

                var userExists = userid is null ? false : await mediator.Send(new UserExists(userid), cancellationToken) ;
                return Results.Ok(userExists);
            })
            .Produces<bool>(StatusCodes.Status200OK)
            .WithName("Get User Exists");

        return routeBuilder;
    }
}