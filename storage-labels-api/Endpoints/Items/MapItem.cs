using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Items;

internal partial class ItemEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("item")
            .WithTags("Items")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapPost("/", CreateItem)
            .WithName("Create Item");

        group.MapGet("/box/{boxId:guid}/", GetItemsByBoxId)
            .WithName("Get Items By Box");

        group.MapGet("/{itemId:guid}", GetItemById)
            .WithName("Get Item");

        group.MapPut("/{itemId:guid}", UpdateItem)
            .WithName("Update Item");

        group.MapDelete("/{itemId:guid}", DeleteItem)
            .WithName("Delete Item");
    }
}
