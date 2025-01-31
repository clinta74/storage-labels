using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Handlers.CommonLocations;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapCommonLocation(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("common-location")
            .WithTags("Common Locations")
            .RequireAuthorization(Policies.Read_CommonLocations)
            .MapCommonLocationEndpoints();
    }

    private static IEndpointRouteBuilder MapCommonLocationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
            .Produces<CommonLocation>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("/", GetCommonLocations)
            .Produces<CommonLocation>(StatusCodes.Status200OK);

        routeBuilder.MapDelete("{commonlocationid}", DeleteCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
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