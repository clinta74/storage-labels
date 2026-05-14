using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Items;

internal static partial class ItemEndpoints
{
    internal static IEndpointRouteBuilder MapItem(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("item")
            .WithTags("Items")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapItemEndpoints();
    }

    private static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapPost("/", CreateItem)
            .WithName("Create Item");

        routeBuilder.MapGet("/box/{boxId:guid}/", GetItemsByBoxId)
            .WithName("Get Items By Box");

        routeBuilder.MapGet("/{itemId:guid}", GetItemById)
            .WithName("Get Item");

        routeBuilder.MapPut("/{itemId:guid}", UpdateItem)
            .WithName("Update Item");

        routeBuilder.MapDelete("/{itemId:guid}", DeleteItem)
            .WithName("Delete Item");

        return routeBuilder;
    }
}
