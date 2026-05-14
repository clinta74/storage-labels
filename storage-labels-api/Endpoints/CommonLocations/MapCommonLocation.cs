using StorageLabelsApi.Models;

namespace StorageLabelsApi.Endpoints.CommonLocations;

internal static partial class CommonLocationEndpoints
{
    internal static IEndpointRouteBuilder MapCommonLocation(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("common-location")
            .WithTags("Common Locations")
            .MapCommonLocationEndpoints();
    }

    private static IEndpointRouteBuilder MapCommonLocationEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
            .WithName("Create Common Location");

        routeBuilder.MapGet("/", GetCommonLocations)
            .WithName("Get Common Locations");

        routeBuilder.MapDelete("{commonLocationId:int}", DeleteCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
            .WithName("Delete Common Location");

        return routeBuilder;
    }
}
