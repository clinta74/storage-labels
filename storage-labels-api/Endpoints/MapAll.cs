namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    internal static IEndpointRouteBuilder MapAll(this IEndpointRouteBuilder routeBuilder)
    {
        var api = routeBuilder.MapGroup("api")
            .RequireAuthorization();
        api.MapBox();
        api.MapCommonLocation();
        api.MapItem();
        api.MapNewUser();
        api.MapLocation();
        api.MapUser();

        routeBuilder.MapGet("health", () => Results.Ok("Hello world."));

        return routeBuilder;
    }
}