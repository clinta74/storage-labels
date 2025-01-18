using System.Runtime.CompilerServices;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Boxes;
using StorageLabelsApi.Handlers.CommonLocations;
using StorageLabelsApi.Handlers.Items;
using StorageLabelsApi.Models.DTO;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapCommonLocation(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("common-location")
            .WithTags("Common Location")
            .RequireAuthorization("write:common-location")
            .MapCommonLocationEndpoints();
    }

    private static IEndpointRouteBuilder MapCommonLocationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateCommonLocation)
            .Produces<CommonLocation>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("/", GetCommonLocations)
            .Produces<CommonLocation>(StatusCodes.Status200OK);

        routeBuilder.MapDelete("{commonlocationid}", DeleteCommonLocation)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound);

        return routeBuilder;
    }

    private static async Task<IResult> CreateCommonLocation(CommonLocationRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var commonLocation = await mediator.Send(new CreateCommonLocation(
            Name: request.Name
        ), cancellationToken);

        return commonLocation
            .ToMinimalApiResult();
    }

    private static IAsyncEnumerable<CommonLocation> GetCommonLocations([FromServices] IMediator mediator, CancellationToken cancellationToken) => 
        mediator.CreateStream(new GetCommonLocation(), cancellationToken);

    private static async Task<IResult> DeleteCommonLocation(int commonLocationId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCommonLocation(commonLocationId), cancellationToken);

        return result.ToMinimalApiResult();
    }
}