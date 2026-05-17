using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Boxes;

internal partial class BoxEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("box")
            .WithTags("Boxes")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapPost("/", CreateBox)
            .WithName("Create Box");

        group.MapGet("{boxId:guid}", GetBoxById)
            .WithName("Get Box By ID");

        group.MapGet("/location/{locationId:long}/", GetBoxesByLocationId)
            .WithName("Get Boxes By Location");

        group.MapPut("{boxId:guid}", UpdateBox)
            .WithName("Update Box");

        group.MapPut("{boxId:guid}/move", MoveBox)
            .WithName("Move Box");

        group.MapDelete("{boxId:guid}", DeleteBox)
            .WithName("Delete Box");
    }
}
