using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Boxes;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Endpoints.V1;

internal static partial class MapEndpoints
{
    internal static IEndpointRouteBuilder MapV1Endpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGroup("v1")
            .MapV1BoxesEndpoints();

        return routeBuilder;
    }

    internal static IEndpointRouteBuilder MapV1BoxesEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/boxes",async  ([FromServices] IMediator mediator, CreateBoxRequest request, CancellationToken cancellationToken) =>
        {
            var UserId = "user";
            var box = await mediator.Send(new CreateBox(
                Code: request.Code,
                Name: request.Name,
                UserId: UserId,
                LocationId: request.LocationId,
                Description: request.Description,
                ImageUrl: request.ImageUrl
            ), cancellationToken);

            return box
                .Map(box => new CreateBoxReponse(box.BoxId))
                .ToMinimalApiResult();
        })
        .Produces<CreateBoxReponse>(StatusCodes.Status201Created)
        .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status409Conflict)
        .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
        .WithName("CreateBox");

        return routeBuilder;
    }
}