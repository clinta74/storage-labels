using StorageLabelsApi.Filters;

namespace StorageLabelsApi.Endpoints.Search;

internal static partial class SearchEndpoints
{
    internal static IEndpointRouteBuilder MapSearch(this IEndpointRouteBuilder routeBuilder)
    {
        return routeBuilder.MapGroup("search")
            .WithTags("Search")
            .RequireRateLimiting("search")
            .AddEndpointFilter<UserExistsEndpointFilter>()
            .MapSearchEndpoints();
    }

    private static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder routeBuilder)
    {
        routeBuilder.MapGet("qrcode/{code}", SearchByQrCode)
            .WithName("Search By QR Code")
            .WithSummary("Search for a box or item by exact QR code match");

        routeBuilder.MapGet("", SearchBoxesAndItems)
            .WithName("Search Boxes And Items")
            .WithSummary("Search boxes and items with full-text search ranking and pagination. Total count returned in x-total-count header.");

        return routeBuilder;
    }
}
