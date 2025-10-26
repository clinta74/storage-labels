using System.Runtime.CompilerServices;
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
            .Produces<BoxResponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status409Conflict)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("{boxid}", GetBoxById)
            .Produces<BoxResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        routeBuilder.MapGet("/location/{locationid}/", GetBoxesByLocationId)
            .Produces<IAsyncEnumerable<BoxResponse>>(StatusCodes.Status200OK);

        routeBuilder.MapPut("{boxId}", UpdateBox)
            .Produces<BoxResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update Box");

        routeBuilder.MapDelete("{boxId}", DeleteBox)
            .Produces(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Delete Box");

        return routeBuilder;
    }

    private static async Task<IResult> GetBoxById(HttpContext context, Guid boxId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new GetBoxById(boxId, userId));

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

    private static async IAsyncEnumerable<BoxResponse> GetBoxesByLocationId(HttpContext context, long locationId, [FromServices] IMediator mediator, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var boxes = mediator.CreateStream(new GetBoxesByLocationId(locationId, userId), cancellationToken);

        await foreach (var box in boxes)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new BoxResponse(box);
        }
    }


    private static async Task<IResult> UpdateBox(HttpContext context, Guid boxId, BoxRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
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

    private static async Task<IResult> DeleteBox(HttpContext context, Guid BoxId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var box = await mediator.Send(new DeleteBox(BoxId, userId), cancellationToken);

        return box.ToMinimalApiResult();
    }
}