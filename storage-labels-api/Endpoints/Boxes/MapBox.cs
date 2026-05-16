using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Boxes;

internal static partial class BoxEndpoints
{
    internal static IEndpointRouteBuilder MapBox(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("box")
            .WithTags("Boxes")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapBoxEndpoints();
    }

    private static IEndpointRouteBuilder MapBoxEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateBox)
            .WithName("Create Box");

        routeBuilder.MapGet("{boxId:guid}", GetBoxById)
            .WithName("Get Box By ID");

        routeBuilder.MapGet("/location/{locationId:long}/", GetBoxesByLocationId)
            .WithName("Get Boxes By Location");

        routeBuilder.MapPut("{boxId:guid}", UpdateBox)
            .WithName("Update Box");

        routeBuilder.MapPut("{boxId:guid}/move", MoveBox)
            .WithName("Move Box");

        routeBuilder.MapDelete("{boxId:guid}", DeleteBox)
            .WithName("Delete Box");

        return routeBuilder;
    }
}
