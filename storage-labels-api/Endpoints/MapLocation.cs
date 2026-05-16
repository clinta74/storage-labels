using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Locations;
using StorageLabelsApi.Models.DTO.Location;
using StorageLabelsApi.Models.DTO.User;
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

        routeBuilder.MapGet("{locationId:long}", GetLocation)
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Get Location");

        routeBuilder.MapPost("/", CreateLocation)
            .Produces<LocationResponse>(StatusCodes.Status201Created)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .WithName("Create Location");

        routeBuilder.MapPut("{locationId:long}", UpdateLocation)
            .Produces<LocationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update Location");

        routeBuilder.MapDelete("{locationId:long}", DeleteLocation)
            .Produces(StatusCodes.Status200OK)
            .Produces<IEnumerable<ProblemDetails>>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Delete Location");

        // User access management
        routeBuilder.MapGet("{locationId:long}/users", GetLocationUsers)
            .Produces<IEnumerable<UserLocationResponse>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Get Location Users");

        routeBuilder.MapPost("{locationId:long}/users", AddUserToLocation)
            .Produces<UserLocationResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict)
            .WithName("Add User To Location");

        routeBuilder.MapPut("{locationId:long}/users/{userId}", UpdateUserLocationAccess)
            .Produces<UserLocationResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Update User Location Access");

        routeBuilder.MapDelete("{locationId:long}/users/{userId}", RemoveUserFromLocation)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithName("Remove User From Location");

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

    private static async Task<IResult> GetLocation(HttpContext context, [FromRoute] long locationId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
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

    private static async Task<IResult> UpdateLocation(HttpContext context, [FromRoute] long locationId, LocationRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();

        var location = await mediator.Send(new UpdateLocation(userid, locationId, request.Name), cancellationToken);
        return location
            .Map(loc => new LocationResponse(loc))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> DeleteLocation(HttpContext context, [FromRoute] long locationId, [FromServices] IMediator mediator, CancellationToken cancellationToken, [FromQuery] bool force = false)
    {
        var userid = context.GetUserId();

        var location = await mediator.Send(new DeleteLocation(userid, locationId, force), cancellationToken);
        return location
            .ToMinimalApiResult();
    }

    private static async Task<IResult> GetLocationUsers(HttpContext context, [FromRoute] long locationId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();
        var result = await mediator.Send(new GetLocationUsers(userid, locationId), cancellationToken);

        return result
            .Map(users => users.Select(u => new UserLocationResponse(u)))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> AddUserToLocation(HttpContext context, [FromRoute] long locationId, AddUserLocationRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var userid = context.GetUserId();
        var result = await mediator.Send(new AddUserToLocation(userid, locationId, request.EmailAddress, request.AccessLevel), cancellationToken);

        return result
            .Map(ul => new UserLocationResponse(ul))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> UpdateUserLocationAccess(HttpContext context, [FromRoute] long locationId, [FromRoute] string userId, UpdateUserLocationRequest request, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var requestUserId = context.GetUserId();
        var result = await mediator.Send(new UpdateUserLocationAccess(requestUserId, locationId, userId, request.AccessLevel), cancellationToken);

        return result
            .Map(ul => new UserLocationResponse(ul))
            .ToMinimalApiResult();
    }

    private static async Task<IResult> RemoveUserFromLocation(HttpContext context, [FromRoute] long locationId, [FromRoute] string userId, [FromServices] IMediator mediator, CancellationToken cancellationToken)
    {
        var requestUserId = context.GetUserId();
        var result = await mediator.Send(new RemoveUserFromLocation(requestUserId, locationId, userId), cancellationToken);

        return result.ToMinimalApiResult();
    }
}
