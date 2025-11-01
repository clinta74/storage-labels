using System.Runtime.CompilerServices;
using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Handlers.CommonLocations;
using StorageLabelsApi.Models;
using StorageLabelsApi.Models.DTO.CommonLocation;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapCommonLocation(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("common-location")
            .WithTags("Common Locations")
            .MapCommonLocationEndpoints();
    }

    private static IEndpointRouteBuilder MapCommonLocationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
            .Produces<CommonLocationResponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ValidationError>>(StatusCodes.Status400BadRequest);

        routeBuilder.MapGet("/", GetCommonLocations)
            .Produces<CommonLocationResponse>(StatusCodes.Status200OK);

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
            .Map(cl => new CommonLocationResponse(cl))
            .ToMinimalApiResult();
    }

    private static async IAsyncEnumerable<CommonLocationResponse> GetCommonLocations([FromServices] IMediator mediator, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var commonLocations = mediator.CreateStream(new GetCommonLocation(), cancellationToken);

        await foreach (var commonLocation in commonLocations)
        {
            if (cancellationToken.IsCancellationRequested) break;
            yield return new CommonLocationResponse(commonLocation);
        }
    }

    private static async Task<IResult> DeleteCommonLocation(int commonLocationId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteCommonLocation(commonLocationId), cancellationToken);

        return result.ToMinimalApiResult();
    }
}