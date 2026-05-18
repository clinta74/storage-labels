namespace StorageLabelsApi.Endpoints;

internal static class EndpointsMapper
{
    internal static WebApplication MapAll(this WebApplication app)
    {
        // Main API
        var api = app.MapGroup("api")
            .RequireAuthorization()
            .WithGroupName("storage-labels-api");

        foreach (var module in app.Services.GetServices<IEndpointModule>())
            module.MapEndpoints(api);

        // Root endpoint - redirect to Swagger in development
        app.MapGet("/", (HttpContext context) =>
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

        app.MapGet("health", () => Results.Ok("Hello world."));

        return app;
    }
}
