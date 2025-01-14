using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Handlers.Users;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapUsers(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("users")
            .MapUsersEndpoints();
    }

    private static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/{userid}", 
        async (
            HttpContext context,
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

        return routeBuilder;
    }
}