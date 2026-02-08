using Ardalis.Result.AspNetCore;
using Asp.Versioning;
using Mediator;
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
        // v1 endpoints
        routeBuilder.MapGet("qrcode/{code}", SearchByQrCode)
            .Produces<SearchResultResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("SearchByQrCodeV1")
            .WithSummary("Search for a box or item by exact QR code match")
            .MapToApiVersion(1.0);

        routeBuilder.MapGet("", SearchBoxesAndItems)
            .Produces<SearchResultsResponse>(StatusCodes.Status200OK)
            .WithName("SearchBoxesAndItems")
            .WithSummary("[DEPRECATED] Search boxes and items - use v2 for better performance and pagination")
            .MapToApiVersion(1.0);

        // v2 endpoints (complete API surface with FTS and pagination)
        routeBuilder.MapGet("qrcode/{code}", SearchByQrCode)
            .Produces<SearchResultResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("SearchByQrCodeV2")
            .WithSummary("Search for a box or item by exact QR code match")
            .MapToApiVersion(2.0);

        routeBuilder.MapGet("", SearchBoxesAndItemsV2)
            .Produces<SearchResultsResponseV2>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithName("SearchBoxesAndItemsV2")
            .WithSummary("Search boxes and items with full-text search ranking and pagination")
            .MapToApiVersion(2.0);

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

    private static async Task<IResult> SearchBoxesAndItemsV2(
        HttpContext context,
        [FromServices] IMediator mediator,
        [FromQuery] string query,
        [FromQuery] long? locationId = null,
        [FromQuery] Guid? boxId = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        // Validate pagination parameters
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var userId = context.GetUserId();

        var result = await mediator.Send(
            new SearchBoxesAndItemsQueryV2(query, userId, locationId, boxId, pageNumber, pageSize), 
            cancellationToken);

        return result.ToMinimalApiResult();
    }
}
