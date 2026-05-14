using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Locations;

internal static partial class LocationEndpoints
{
    internal static IEndpointRouteBuilder MapLocation(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("location")
            .WithTags("Location")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapLocationEndpoints();
    }

    private static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("/", GetLocationsByUserId)
            .WithName("Get Current User Locations");

        routeBuilder.MapGet("{locationId:long}", GetLocation)
            .WithName("Get Location");

        routeBuilder.MapPost("/", CreateLocation)
            .WithName("Create Location");

        routeBuilder.MapPut("{locationId:long}", UpdateLocation)
            .WithName("Update Location");

        routeBuilder.MapDelete("{locationId:long}", DeleteLocation)
            .WithName("Delete Location");

        routeBuilder.MapGet("{locationId:long}/users", GetLocationUsers)
            .WithName("Get Location Users");

        routeBuilder.MapPost("{locationId:long}/users", AddUserToLocation)
            .WithName("Add User To Location");

        routeBuilder.MapPut("{locationId:long}/users/{userId}", UpdateUserLocationAccess)
            .WithName("Update User Location Access");

        routeBuilder.MapDelete("{locationId:long}/users/{userId}", RemoveUserFromLocation)
            .WithName("Remove User From Location");

        return routeBuilder;
    }
}
