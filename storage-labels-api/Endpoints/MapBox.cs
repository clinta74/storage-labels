using System.Runtime.CompilerServices;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Boxes;
using StorageLabelsApi.Models.DTO.Box;
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
            .Produces<BoxResponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status409Conflict)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
            .WithName("Create Box");

        routeBuilder.MapGet("{boxId:guid}", GetBoxById)
            .Produces<BoxResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Get Box By ID");

        routeBuilder.MapGet("/location/{locationId:long}/", GetBoxesByLocationId)
            .Produces<IAsyncEnumerable<BoxResponse>>(StatusCodes.Status200OK)
            .WithName("Get Boxes By Location");

        routeBuilder.MapPut("{boxId:guid}", UpdateBox)
            .Produces<BoxResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update Box");

        routeBuilder.MapPut("{boxId:guid}/move", MoveBox)
            .Produces<BoxResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Move Box");

        routeBuilder.MapDelete("{boxId:guid}", DeleteBox)
            .Produces(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Delete Box");

        return routeBuilder;
    }

    private static async Task<IResult> GetBoxById(HttpContext context, [FromRoute] Guid boxId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new GetBoxById(boxId, userId), cancellationToken);

        return box
            .Map(box => new BoxResponse(box))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> CreateBox(HttpContext context, BoxRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new CreateBox(
            Code: request.Code,
            Name: request.Name,
            UserId: userId,
            LocationId: request.LocationId,
            Description: request.Description,
            ImageUrl: request.ImageUrl,
            ImageMetadataId: request.ImageMetadataId
        ), cancellationToken);

        return box
            .Map(box => new BoxResponse(box))
            .ToMinimalApiResult();
    }

    private static async IAsyncEnumerable<BoxResponse> GetBoxesByLocationId(HttpContext context, [FromRoute] long locationId, [FromServices] IMediator mediator, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var boxes = mediator.CreateStream(new GetBoxesByLocationId(locationId, userId), cancellationToken);

        await foreach (var box in boxes)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new BoxResponse(box);
        }
    }


    private static async Task<IResult> UpdateBox(HttpContext context, [FromRoute] Guid boxId, BoxRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var result = await mediator.Send(new UpdateBox(
            BoxId: boxId,
            Code: request.Code,
            Name: request.Name,
            UserId: userId,
            LocationId: request.LocationId,
            Description: request.Description,
            ImageUrl: request.ImageUrl,
            ImageMetadataId: request.ImageMetadataId
        ), cancellationToken);

        return result
            .Map(box => new BoxResponse(box))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> MoveBox(HttpContext context, [FromRoute] Guid BoxId, [FromBody] MoveBoxRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var result = await mediator.Send(new MoveBox(BoxId, request.DestinationLocationId, userId), cancellationToken);

        return result
            .Map(box => new BoxResponse(box))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> DeleteBox(HttpContext context, [FromRoute] Guid BoxId, [FromServices] IMediator mediator, CancellationToken cancellationToken, [FromQuery] bool force = false)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new DeleteBox(BoxId, userId, force), cancellationToken);

        return box.ToMinimalApiResult();
    }
}
