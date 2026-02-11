using Microsoft.AspNetCore.Http.Features;

namespace StorageLabelsApi.Middleware;

/// <summary>
/// Middleware that adds Sunset HTTP header to API v1 endpoints
/// to indicate deprecation timeline per RFC 8594.
/// </summary>
public class ApiSunsetMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiSunsetMiddleware> _logger;
    
    // v1 API sunset date: 6 months from v2 release (August 7, 2026)
    private static readonly DateTimeOffset SunsetDate = new(2026, 8, 7, 0, 0, 0, TimeSpan.Zero);
    private const string SunsetHeaderName = "Sunset";
    private const string DeprecationHeaderName = "Deprecation";
    private const string LinkHeaderName = "Link";
    
    public ApiSunsetMiddleware(RequestDelegate next, ILogger<ApiSunsetMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if this is a v1 API endpoint (simple path-based detection for now)
        // v1 endpoints: /api/search/* (legacy, no version prefix)
        var isV1Endpoint = context.Request.Path.StartsWithSegments("/api/search") && 
                          !context.Request.Path.StartsWithSegments("/api/v2/search");
        
        if (isV1Endpoint)
        {
            // Add Sunset header (RFC 8594)
            context.Response.Headers[SunsetHeaderName] = SunsetDate.ToString("R");
            
            // Add Deprecation header (draft standard)
            context.Response.Headers[DeprecationHeaderName] = "true";
            
            // Add Link header pointing to migration documentation
            context.Response.Headers[LinkHeaderName] = 
                "</docs/api-migration-v1-to-v2>; rel=\"deprecation\"; type=\"text/html\"";
            
            _logger.LogDebug(
                "Added sunset headers to v1 endpoint: {Path}. Sunset date: {SunsetDate}",
                context.Request.Path,
                SunsetDate);
        }
        
        await _next(context);
    }
}

/// <summary>
/// Extension method to register ApiSunsetMiddleware in the pipeline.
/// </summary>
public static class ApiSunsetMiddlewareExtensions
{
    public static IApplicationBuilder UseApiSunset(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiSunsetMiddleware>();
    }
}
