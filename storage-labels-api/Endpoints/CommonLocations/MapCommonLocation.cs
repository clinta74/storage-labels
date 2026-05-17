using StorageLabelsApi.Models;

namespace StorageLabelsApi.Endpoints.CommonLocations;

internal partial class CommonLocationEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("common-location")
            .WithTags("Common Locations");

        group.MapPost("/", CreateCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
            .WithName("Create Common Location");

        group.MapGet("/", GetCommonLocations)
            .WithName("Get Common Locations");

        group.MapDelete("{commonLocationId:int}", DeleteCommonLocation)
            .RequireAuthorization(Policies.Write_CommonLocations)
            .WithName("Delete Common Location");
    }
}
