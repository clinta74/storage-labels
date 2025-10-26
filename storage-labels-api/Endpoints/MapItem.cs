using System.Runtime.CompilerServices;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Items;
using StorageLabelsApi.Models.DTO;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapItem(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("item")
            .WithTags("Items")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapItemEndpoints();
    }

    private static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateItem)
            .Produces<ItemResponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status409Conflict)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("/box/{boxid:guid}/", GetItemsByBoxId)
            .Produces<IAsyncEnumerable<ItemResponse>>(StatusCodes.Status200OK);

        // New endpoints for get, update, delete by ItemId
        routeBuilder.MapGet("/{itemId:guid}", GetItemById)
            .Produces<ItemResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        routeBuilder.MapPut("/{itemId:guid}", UpdateItem)
            .Produces<ItemResponse>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        routeBuilder.MapDelete("/{itemId:guid}", DeleteItem)
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> CreateItem(HttpContext context, ItemRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var item = await mediator.Send(new CreateItem(
            UserId: userId,
            BoxId: request.BoxId,
            Name: request.Name,
            Description: request.Description,
            ImageUrl: request.ImageUrl,
            ImageMetadataId: request.ImageMetadataId
        ));

        return item
            .Map(item => new ItemResponse(item))
            .ToMinimalApiResult();
    }

    private static async IAsyncEnumerable<ItemResponse> GetItemsByBoxId(HttpContext context, Guid boxId, [FromServices] IMediator mediator, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var items = mediator.CreateStream(new GetItemsByBoxId(boxId, userId), cancellationToken);

        await foreach (var item in items)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new ItemResponse(item);
        }
    }

    private static async Task<IResult> GetItemById(HttpContext context, Guid itemId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var item = await mediator.Send(new GetItemById(itemId, userId), cancellationToken);

        return item
            .Map(item => new ItemResponse(item))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> UpdateItem(HttpContext context, Guid itemId, ItemRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var item = await mediator.Send(new UpdateItem(
            ItemId: itemId,
            UserId: userId,
            BoxId: request.BoxId,
            Name: request.Name,
            Description: request.Description,
            ImageUrl: request.ImageUrl,
            ImageMetadataId: request.ImageMetadataId
        ), cancellationToken);

        return item
            .Map(item => new ItemResponse(item))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> DeleteItem(HttpContext context, Guid itemId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();
        var item = await mediator.Send(new DeleteItem(itemId, userId), cancellationToken);

        return item.ToMinimalApiResult();
    }
}