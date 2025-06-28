using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Locations;
using StorageLabelsApi.Models.DTO;
using IResult = Microsoft.AspNetCore.Http.IResult;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    private static IEndpointRouteBuilder MapLocation(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("location")
            .WithTags("Location")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapLocationEndpoints();
    }

    private static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/", GetLocationsByUserId)
            .Produces<IEnumerable<LocationResponse>>(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .WithName("Get Current User Locations");

        routeBuilder.MapGet("{locationId}", GetLocation)
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Get Location");

        routeBuilder.MapPost("/", CreateLocation)
            .Produces<LocationResponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .WithName("Create Location");

        routeBuilder.MapPut("{locationId}", UpdateLocation)
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update Location");

        routeBuilder.MapDelete("{locationId}", DeleteLocation)
            .Produces(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Delete Location");

        return routeBuilder;
    }
    private static async Task<IResult> GetLocationsByUserId(HttpContext context, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();
        var locations = await mediator.Send(new GetLocationsByUserId(userid), cancellationToken);

        return locations
            .Map(locs => locs.Select(loc => new LocationResponse(loc)))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> GetLocation(HttpContext context, long locationId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();
        var location = await mediator.Send(new GetLocation(userid, locationId), cancellationToken);

        return location
            .Map(loc => new LocationResponse(loc))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> CreateLocation(HttpContext context, LocationRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();

        var location = await mediator.Send(new CreateLocation(userid, request.Name), cancellationToken);
        return location
            .Map(loc => new LocationResponse(loc))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> UpdateLocation(HttpContext context, long LocationId, LocationRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();

        var location = await mediator.Send(new UpdateLocation(userid, LocationId, request.Name), cancellationToken);
        return location
            .Map(loc => new LocationResponse(loc))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> DeleteLocation(HttpContext context, long LocationId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();

        var location = await mediator.Send(new DeleteLocation(userid, LocationId), cancellationToken);
        return location
            .ToMinimalApiResult();
    }
}