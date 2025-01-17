using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Boxes;
using StorageLabelsApi.Models.DTO;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapBox(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("box")
            .WithTags("Boxes")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapBoxEndpoints();
    }

    private static IEndpointRouteBuilder MapBoxEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateBox)
            .Produces<BoxReponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status409Conflict)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("{boxid}", GetBoxById)
            .Produces<BoxReponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> GetBoxById(HttpContext context, Guid boxId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new GetBoxById(boxId, userId));
        
        return box
            .Map(box => new BoxReponse(box))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> CreateBox(HttpContext context, CreateBoxRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new CreateBox(
            Code: request.Code,
            Name: request.Name,
            UserId: userId,
            LocationId: request.LocationId,
            Description: request.Description,
            ImageUrl: request.ImageUrl
        ), cancellationToken);

        return box
            .Map(box => new BoxReponse(box))
            .ToMinimalApiResult();
    }
}