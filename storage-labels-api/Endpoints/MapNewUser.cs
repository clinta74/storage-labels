using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Handlers.NewUsers;
using StorageLabelsApi.Models.DTO.NewUser;
using IResult = Microsoft.AspNetCore.Http.IResult;

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
        routeBuilder.MapGet("/", CreateNewUser)
            .Produces<GetNewUserResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status404NotFound)
            .WithName("Get New Auth0 User");

        return routeBuilder;
    }

    private static async Task<IResult> CreateNewUser(HttpContext context, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.TryGetUserId();
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
    }
}