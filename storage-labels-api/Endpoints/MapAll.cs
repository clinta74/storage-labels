namespace StorageLabelsApi.Endpoints;

internal static partial class EndpointsMapper
{
    internal static IEndpointRouteBuilder MapAll(this IEndpointRouteBuilder routeBuilder)
    {
        // Map public authentication endpoints (no authorization required)
        routeBuilder.MapAuthenticationEndpoints();

        // Main API
        var api = routeBuilder.MapGroup("api")
            .RequireAuthorization()
            .WithGroupName("storage-labels-api");
        
        api.MapBox();
        api.MapCommonLocation();
        api.MapItem();
        api.MapLocation();
        api.MapUser();
        api.MapImageEndpoints();
        api.MapSearch();
        api.MapEncryptionKeyEndpoints();

        // Root endpoint - redirect to Swagger in development
        routeBuilder.MapGet("/", (HttpContext context) =>
        {
            var env = context.RequestServices.GetRequiredService<IHostEnvironment>();
            if (env.IsDevelopment())
            {
                return Results.Redirect("/swagger");
            }
            return Results.Ok(new { 
                message = "Storage Labels API", 
                version = "1.0",
                health = "/health",
                swagger = "/swagger"
            });
        })
        .WithName("Root")
        .WithSummary("API root endpoint")
        .ExcludeFromDescription();

        routeBuilder.MapGet("health", () => Results.Ok("Hello world."));

        return routeBuilder;
    }
}
