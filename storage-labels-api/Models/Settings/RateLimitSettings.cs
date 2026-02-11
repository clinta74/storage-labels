namespace StorageLabelsApi.Models.Settings;

/// <summary>
/// Configuration settings for rate limiting across the API.
/// Different policies control access to various endpoint groups.
/// </summary>
public class RateLimitSettings
{
    /// <summary>
    /// Global rate limit settings applied to all endpoints by default.
    /// Uses a sliding window algorithm for smooth distribution.
    /// </summary>
    public GlobalRateLimitSettings Global { get; set; } = new();

    /// <summary>
    /// Rate limit settings for authentication endpoints.
    /// Uses a fixed window algorithm with strict limits to prevent brute force attacks.
    /// </summary>
    public AuthRateLimitSettings Auth { get; set; } = new();

    /// <summary>
    /// Rate limit settings for search endpoints.
    /// Uses a token bucket algorithm to allow burst traffic while maintaining average rate.
    /// </summary>
    public SearchRateLimitSettings Search { get; set; } = new();

    /// <summary>
    /// Rate limit settings for image endpoints.
    /// Uses a concurrency limiter to control simultaneous operations.
    /// </summary>
    public ImageRateLimitSettings Images { get; set; } = new();
}

/// <summary>
/// Global rate limiting settings using sliding window algorithm.
/// </summary>
public class GlobalRateLimitSettings
{
    /// <summary>
    /// Maximum number of requests allowed within the time window.
    /// Default: 20 requests per window.
    /// </summary>
    public int PermitLimit { get; set; } = 20;

    /// <summary>
    /// Time window in seconds for the sliding window.
    /// Default: 10 seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 10;

    /// <summary>
    /// Number of segments to divide the window into for smoother distribution.
    /// Default: 2 segments (checks every 5 seconds for 10-second window).
    /// </summary>
    public int SegmentsPerWindow { get; set; } = 2;

    /// <summary>
    /// Maximum number of requests to queue when limit is reached.
    /// Default: 5.
    /// </summary>
    public int QueueLimit { get; set; } = 5;
}

/// <summary>
/// Authentication rate limiting settings using fixed window algorithm.
/// </summary>
public class AuthRateLimitSettings
{
    /// <summary>
    /// Maximum number of authentication requests allowed per time window.
    /// Default: 3 requests per window (strict to prevent brute force).
    /// </summary>
    public int PermitLimit { get; set; } = 3;

    /// <summary>
    /// Time window in seconds for the fixed window.
    /// Default: 10 seconds.
    /// </summary>
    public int WindowSeconds { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests to queue when limit is reached.
    /// Default: 0 (no queueing for auth - fail fast).
    /// </summary>
    public int QueueLimit { get; set; } = 0;
}

/// <summary>
/// Search rate limiting settings using token bucket algorithm.
/// </summary>
public class SearchRateLimitSettings
{
    /// <summary>
    /// Initial number of tokens available (allows burst traffic).
    /// Default: 20 tokens.
    /// </summary>
    public int TokenLimit { get; set; } = 20;

    /// <summary>
    /// Number of tokens to add per replenishment period.
    /// Default: 5 tokens per period.
    /// </summary>
    public int TokensPerPeriod { get; set; } = 5;

    /// <summary>
    /// How often to replenish tokens in seconds.
    /// Default: 5 seconds.
    /// </summary>
    public int ReplenishmentPeriodSeconds { get; set; } = 5;

    /// <summary>
    /// Maximum number of requests to queue when tokens depleted.
    /// Default: 5.
    /// </summary>
    public int QueueLimit { get; set; } = 5;
}

/// <summary>
/// Image rate limiting settings using concurrency limiter.
/// </summary>
public class ImageRateLimitSettings
{
    /// <summary>
    /// Maximum number of concurrent image operations per user.
    /// Default: 2 concurrent operations.
    /// </summary>
    public int PermitLimit { get; set; } = 2;

    /// <summary>
    /// Maximum number of requests to queue when concurrency limit reached.
    /// Default: 1.
    /// </summary>
    public int QueueLimit { get; set; } = 1;
}
