using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Boxes;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapBox(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("box")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapBoxEndpoints();
    }

    private static IEndpointRouteBuilder MapBoxEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/",
            async (
                HttpContext context,
                CreateBoxRequest request,
                [FromServices] IMediator mediator,
                CancellationToken cancellationToken
            ) =>
            {
                var userId = context.GetUserId();
                if (userId is null)
                {
                    return Results.BadRequest("User id not found.");
                }

                var box = await mediator.Send(new CreateBox(
                    Code: request.Code,
                    Name: request.Name,
                    UserId: userId,
                    LocationId: request.LocationId,
                    Description: request.Description,
                    ImageUrl: request.ImageUrl
                ), cancellationToken);

                return box
                    .Map(box => new CreateBoxReponse(box.BoxId))
                    .ToMinimalApiResult();
            }
        )
        .Produces<CreateBoxReponse>(StatusCodes.Status201Created)
        .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status409Conflict)
        .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
        .WithName("Create Box");

        return routeBuilder;
    }
}