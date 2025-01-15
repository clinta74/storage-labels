using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Handlers.NewUsers;
using StorageLabelsApi.Models.DTO.NewUser;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapNewUser(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("new-user")
            .WithTags("New User")
            .MapNewUsersEndpoints();
    }

    private static IEndpointRouteBuilder MapNewUsersEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/",
        async (
            HttpContext context,
            [FromServices] IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                var userId = context.GetUserId();
                if (userId is null)
                {
                    return Results.NotFound("Current user could not be found.");
                }
                var user = await mediator.Send(new GetNewUser(userId), cancellationToken);

                return user
                    .Map(user => new GetNewUserResponse(
                        user.FirstName,
                        user.LastName,
                        user.Email)
                    )
                    .ToMinimalApiResult();

            })
            .Produces<GetNewUserResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .WithName("Get New Auth0 User");

        return routeBuilder;
    }
}