using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using StorageLabelsApi.Models.Settings;

namespace StorageLabelsApi.Extensions;

/// <summary>
/// Extension methods for configuring rate limiting middleware.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Adds and configures rate limiting with multiple policies.
    /// Settings can be overridden in appsettings.json under "RateLimit" section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddConfiguredRateLimiting(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Load rate limit settings from configuration with defaults
        var rateLimitSettings = configuration.GetSection("RateLimit").Get<RateLimitSettings>() 
            ?? new RateLimitSettings();

        services.AddRateLimiter(options =>
        {
            // Default policy: Sliding window for general API requests
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? 
                             context.Connection.RemoteIpAddress?.ToString() ?? 
                             "anonymous";
                
                return RateLimitPartition.GetSlidingWindowLimiter(userId, key => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitSettings.Global.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitSettings.Global.WindowSeconds),
                    SegmentsPerWindow = rateLimitSettings.Global.SegmentsPerWindow,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitSettings.Global.QueueLimit
                });
            });
            
            // Policy for authentication endpoints (stricter)
            options.AddPolicy("auth", context =>
            {
                var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                
                return RateLimitPartition.GetFixedWindowLimiter(ipAddress, key => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitSettings.Auth.PermitLimit,
                    Window = TimeSpan.FromSeconds(rateLimitSettings.Auth.WindowSeconds),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = rateLimitSettings.Auth.QueueLimit
                });
            });
            
            // Policy for search (token bucket for burst handling)
            options.AddPolicy("search", context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                
                return RateLimitPartition.GetTokenBucketLimiter(userId, key => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = rateLimitSettings.Search.TokenLimit,
                    TokensPerPeriod = rateLimitSettings.Search.TokensPerPeriod,
                    ReplenishmentPeriod = TimeSpan.FromSeconds(rateLimitSettings.Search.ReplenishmentPeriodSeconds),
                    QueueLimit = rateLimitSettings.Search.QueueLimit
                });
            });
            
            // Policy for image operations (concurrency limit)
            options.AddPolicy("images", context =>
            {
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
                
                return RateLimitPartition.GetConcurrencyLimiter(userId, key => new ConcurrencyLimiterOptions
                {
                    PermitLimit = rateLimitSettings.Images.PermitLimit,
                    QueueLimit = rateLimitSettings.Images.QueueLimit
                });
            });
            
            // Custom rejection response
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                
                TimeSpan? retryAfter = null;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
                {
                    retryAfter = retryAfterValue;
                    context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfterValue.TotalSeconds).ToString();
                }
                
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests",
                    message = "Rate limit exceeded. Please try again later.",
                    retryAfter = retryAfter?.TotalSeconds
                }, cancellationToken);
            };
        });

        return services;
    }
}
