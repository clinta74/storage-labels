namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    internal static IEndpointRouteBuilder MapAll(this IEndpointRouteBuilder routeBuilder)
    {
        // Map public authentication endpoints (no authorization required)
        routeBuilder.MapAuthenticationEndpoints();

        var api = routeBuilder.MapGroup("api")
            .RequireAuthorization();
        api.MapBox();
        api.MapCommonLocation();
        api.MapItem();
        api.MapLocation();
        api.MapUser();
        api.MapImageEndpoints();
        api.MapSearch();
        api.MapEncryptionKeyEndpoints();

        routeBuilder.MapGet("health", () => Results.Ok("Hello world."));

        return routeBuilder;
    }
}