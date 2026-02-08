namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    internal static IEndpointRouteBuilder MapAll(this IEndpointRouteBuilder routeBuilder)
    {
        // Map public authentication endpoints (no authorization required)
        routeBuilder.MapAuthenticationEndpoints();

        // v1 API (default, no version in URL)
        var apiV1 = routeBuilder.MapGroup("api")
            .RequireAuthorization()
            .WithApiVersionSet()
            .HasApiVersion(1.0);
        
        apiV1.MapBox();
        apiV1.MapCommonLocation();
        apiV1.MapItem();
        apiV1.MapLocation();
        apiV1.MapUser();
        apiV1.MapImageEndpoints();
        apiV1.MapSearch();
        apiV1.MapEncryptionKeyEndpoints();

        // v2 API (explicit version in URL)
        var apiV2 = routeBuilder.MapGroup("api/v2")
            .RequireAuthorization()
            .WithApiVersionSet()
            .HasApiVersion(2.0);
        
        apiV2.MapBox();
        apiV2.MapCommonLocation();
        apiV2.MapItem();
        apiV2.MapLocation();
        apiV2.MapUser();
        apiV2.MapImageEndpoints();
        apiV2.MapSearch();
        apiV2.MapEncryptionKeyEndpoints();

        routeBuilder.MapGet("health", () => Results.Ok("Hello world."));

        return routeBuilder;
    }
}