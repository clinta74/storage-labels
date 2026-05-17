using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Locations;

internal partial class LocationEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("location")
            .WithTags("Location")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapGet("/", GetLocationsByUserId)
            .WithName("Get Current User Locations");

        group.MapGet("{locationId:long}", GetLocation)
            .WithName("Get Location");

        group.MapPost("/", CreateLocation)
            .WithName("Create Location");

        group.MapPut("{locationId:long}", UpdateLocation)
            .WithName("Update Location");

        group.MapDelete("{locationId:long}", DeleteLocation)
            .WithName("Delete Location");

        group.MapGet("{locationId:long}/users", GetLocationUsers)
            .WithName("Get Location Users");

        group.MapPost("{locationId:long}/users", AddUserToLocation)
            .WithName("Add User To Location");

        group.MapPut("{locationId:long}/users/{userId}", UpdateUserLocationAccess)
            .WithName("Update User Location Access");

        group.MapDelete("{locationId:long}/users/{userId}", RemoveUserFromLocation)
            .WithName("Remove User From Location");
    }
}
