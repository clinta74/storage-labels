using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    // Search operations: EventId range 12000-12999
    
    // Full-text search operations (12000-12099)
    [LoggerMessage(
        EventId = 12000,
        Level = LogLevel.Information,
        Message = "Starting FTS search: Query='{Query}', UserId='{UserId}', LocationId={LocationId}, BoxId={BoxId}, Page={PageNumber}, Size={PageSize}")]
    public static partial void SearchStarted(
        this ILogger logger,
        string query,
        string userId,
        long? locationId,
        Guid? boxId,
        int pageNumber,
        int pageSize);

    [LoggerMessage(
        EventId = 12001,
        Level = LogLevel.Information,
        Message = "FTS search completed: Query='{Query}', TotalResults={TotalResults}, PagedResults={PagedResults}, Duration={Duration}ms")]
    public static partial void SearchCompleted(
        this ILogger logger,
        string query,
        int totalResults,
        int pagedResults,
        long duration);

    [LoggerMessage(
        EventId = 12002,
        Level = LogLevel.Warning,
        Message = "FTS search returned no results: Query='{Query}', UserId='{UserId}'")]
    public static partial void SearchNoResults(
        this ILogger logger,
        string query,
        string userId);

    [LoggerMessage(
        EventId = 12003,
        Level = LogLevel.Error,
        Message = "FTS search failed: Query='{Query}', UserId='{UserId}'")]
    public static partial void SearchFailed(
        this ILogger logger,
        Exception exception,
        string query,
        string userId);

    // QR code search operations (12100-12199)
    [LoggerMessage(
        EventId = 12100,
        Level = LogLevel.Information,
        Message = "QR code search: Code='{Code}', UserId='{UserId}'")]
    public static partial void QrCodeSearchStarted(
        this ILogger logger,
        string code,
        string userId);

    [LoggerMessage(
        EventId = 12101,
        Level = LogLevel.Information,
        Message = "QR code search found: Code='{Code}', Type='{Type}', Id='{Id}'")]
    public static partial void QrCodeSearchFound(
        this ILogger logger,
        string code,
        string type,
        string id);

    [LoggerMessage(
        EventId = 12102,
        Level = LogLevel.Information,
        Message = "QR code search not found: Code='{Code}', UserId='{UserId}'")]
    public static partial void QrCodeSearchNotFound(
        this ILogger logger,
        string code,
        string userId);

    // Legacy LIKE search operations (12200-12299)
    [LoggerMessage(
        EventId = 12200,
        Level = LogLevel.Information,
        Message = "Legacy LIKE search (v1): Query='{Query}', UserId='{UserId}', LocationId={LocationId}, BoxId={BoxId}")]
    public static partial void LegacySearchStarted(
        this ILogger logger,
        string query,
        string userId,
        long? locationId,
        Guid? boxId);

    [LoggerMessage(
        EventId = 12201,
        Level = LogLevel.Information,
        Message = "Legacy LIKE search completed: Query='{Query}', Results={ResultCount}")]
    public static partial void LegacySearchCompleted(
        this ILogger logger,
        string query,
        int resultCount);
}

