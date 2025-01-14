using Ardalis.Result.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using StorageLabelsApi.Filters;
using StorageLabelsApi.Handlers.Boxes;
using StorageLabelsApi.Models.DTO;

namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    internal static IEndpointRouteBuilder MapAll(this IEndpointRouteBuilder routeBuilder)
    {
        var api = routeBuilder.MapGroup("api");
        api.MapBoxes();
        api.MapUsers();

        api.MapGet("test", () => Results.Ok("Hello world."));

        return routeBuilder;
    }
}