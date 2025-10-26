using Microsoft.Extensions.Logging;
using StorageLabelsApi.Logging;
using System;
using System.Collections.Generic;

namespace StorageLabelsApi.Filters;

public class ImageAccessFilter : IEndpointFilter
{
    private readonly RateLimiter _rateLimiter;
    private readonly ILogger<ImageAccessFilter> _logger;
    private readonly IConfiguration _configuration;

    public ImageAccessFilter(RateLimiter rateLimiter, ILogger<ImageAccessFilter> logger, IConfiguration configuration)
    {
        _rateLimiter = rateLimiter;
        _logger = logger;
        _configuration = configuration;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var userId = httpContext.User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value ?? string.Empty;
        var hostValue = httpContext.Request.Host.Value ?? string.Empty;

        // Rate limiting (per user)
        if (!_rateLimiter.Allow(userId))
        {
            _logger.LogImageRateLimitExceeded(userId);
            return Results.StatusCode(429);
        }

        // Anti-hotlinking: Only allow requests with an Origin or Referer from our own domain or allowed origins
        var referer = httpContext.Request.Headers["Referer"].ToString();
        var origin = httpContext.Request.Headers["Origin"].ToString();

        // Get allowed origins from configuration (same as CORS)
        var allowedOrigins = new[] 
        { 
            "http://localhost:4000", 
            "https://storage-labels.pollyspeople.net",
            hostValue // Also allow the API's own host
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

public class RateLimiter
{
    private readonly int _maxRequests;
    private readonly TimeSpan _interval;
    private readonly Dictionary<string, Queue<DateTime>> _requests = new();
    private readonly object _lock = new();

    public RateLimiter(int maxRequests, TimeSpan interval)
    {
        _maxRequests = maxRequests;
        _interval = interval;
    }

    public bool Allow(string userId)
    {
        lock (_lock)
        {
            if (!_requests.TryGetValue(userId, out var queue))
            {
                queue = new Queue<DateTime>();
                _requests[userId] = queue;
            }
            var now = DateTime.UtcNow;
            while (queue.Count > 0 && (now - queue.Peek()) > _interval)
                queue.Dequeue();
            if (queue.Count >= _maxRequests)
                return false;
            queue.Enqueue(now);
            return true;
        }
    }
}
