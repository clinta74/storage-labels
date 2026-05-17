using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Search;

internal partial class SearchEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder routeBuilder)
    {
        var group = routeBuilder.MapGroup("search")
            .WithTags("Search")
            .RequireRateLimiting("search")
            .AddEndpointFilter<UserExistsEndpointFilter>();

        group.MapGet("qrcode/{code}", SearchByQrCode)
            .WithName("Search By QR Code")
            .WithSummary("Search for a box or item by exact QR code match");

        group.MapGet("", SearchBoxesAndItems)
            .WithName("Search Boxes And Items")
            .WithSummary("Search boxes and items with full-text search ranking and pagination. Total count returned in x-total-count header.");
    }
}
