using Microsoft.Extensions.Logging;
using StorageLabelsApi.Logging;

namespace StorageLabelsApi.Filters;

/// <summary>
/// Filter for image endpoint access control. Validates anti-hotlinking protection.
/// Rate limiting is now handled by built-in .NET rate limiting middleware.
/// </summary>
public class ImageAccessFilter : IEndpointFilter
{
    private readonly ILogger<ImageAccessFilter> _logger;
    private readonly IConfiguration _configuration;

    public ImageAccessFilter(ILogger<ImageAccessFilter> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var userId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;
        var hostValue = httpContext.Request.Host.Value ?? string.Empty;

        // Anti-hotlinking: Only allow requests with an Origin or Referer from our own domain or allowed origins
        var referer = httpContext.Request.Headers["Referer"].ToString();
        var origin = httpContext.Request.Headers["Origin"].ToString();

        // Get allowed origins from configuration (same as CORS)
        var allowedOrigins = new[] 
        { 
            "http://localhost:4000", 
            "https://storage-labels.pollyspeople.net",
            $"http://{hostValue}",  // Allow the API's own host with http
            $"https://{hostValue}"  // Allow the API's own host with https
        };
        
        if (!string.IsNullOrEmpty(referer) && !IsAllowedOrigin(referer, allowedOrigins))
        {
            _logger.LogImageHotlinkReferer(userId, referer);
            return Results.Forbid();
        }
        if (!string.IsNullOrEmpty(origin) && !IsAllowedOrigin(origin, allowedOrigins))
        {
            _logger.LogImageHotlinkOrigin(userId, origin);
            return Results.Forbid();
        }

        return await next(context);
    }

    private static bool IsAllowedOrigin(string url, string[] allowedOrigins)
    {
        foreach (var allowedOrigin in allowedOrigins)
        {
            if (url.StartsWith(allowedOrigin, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}