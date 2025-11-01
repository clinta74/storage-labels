using Ardalis.Result.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Extensions;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Search;
using StorageLabelsApi.Models.DTO.Search;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapSearch(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("search")
            .WithTags("Search")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapSearchEndpoints();
    }

    private static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("qrcode/{code}", SearchByQrCode)
            .Produces<SearchResultResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("SearchByQrCode")
            .WithSummary("Search for a box or item by exact QR code match");

        routeBuilder.MapGet("", SearchBoxesAndItems)
            .Produces<SearchResultsResponse>(StatusCodes.Status200OK)
            .WithName("SearchBoxesAndItems")
            .WithSummary("Search boxes and items by name, code, or description");

        return routeBuilder;
    }

    private static async Task<IResult> SearchByQrCode(
        HttpContext context,
        string code,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var result = await mediator.Send(new SearchByQrCodeQuery(code, userId), cancellationToken);

        return result.ToMinimalApiResult();
    }

    private static async Task<IResult> SearchBoxesAndItems(
        HttpContext context,
        [FromQuery] string query,
        [FromQuery] long? locationId,
        [FromQuery] Guid? boxId,
        [FromServices] IMediator mediator,
        CancellationToken cancellationToken)
    {
        var userId = context.GetUserId();

        var result = await mediator.Send(new SearchBoxesAndItemsQuery(query, userId, locationId, boxId), cancellationToken);

        return result.ToMinimalApiResult();
    }
}
