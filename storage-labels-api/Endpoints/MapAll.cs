using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Handlers.Users;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    internal static IEndpointRouteBuilder MapAll(this IEndpointRouteBuilder routeBuilder)
    {
        var api = routeBuilder.MapGroup("api")
            .RequireAuthorization();
        api.MapBox();
        api.MapUser();

        routeBuilder.MapGet("health", () => Results.Ok("Hello world."));

        return routeBuilder;
    }
}