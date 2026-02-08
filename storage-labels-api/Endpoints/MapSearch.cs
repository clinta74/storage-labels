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
        routeBuilder.MapGet("qrcode/{code}", SearchByQrCode)
            .Produces<SearchResultResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Search By QR Code")
            .WithSummary("Search for a box or item by exact QR code match");

        routeBuilder.MapGet("", SearchBoxesAndItems)
            .Produces<List<SearchResultResponse>>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .WithName("Search Boxes And Items")
            .WithSummary("Search boxes and items with full-text search ranking and pagination. Total count returned in x-total-count header.");

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
            new SearchBoxesAndItemsQuery(query, userId, locationId, boxId, pageNumber, pageSize), cancellationToken);

        if (result.IsSuccess)
        {
            context.Response.Headers["x-total-count"] = result.Value.TotalResults.ToString();
            
            var response = result.Value.Results
                .Select(r => new SearchResultResponse(r))
                .ToList();
                
            return Results.Ok(response);
        }
        
        return Results.BadRequest(result.Errors);
    }
}
