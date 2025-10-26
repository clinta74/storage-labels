using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Image {ImageId} uploaded by user {UserId} with filename {FileName}")]
    public static partial void LogImageUploaded(
        this ILogger logger,
        Guid imageId,
        string userId,
        string fileName);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Image {ImageId} deleted by user {UserId}")]
    public static partial void LogImageDeleted(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Failed to delete image file {ImageId}")]
    public static partial void LogImageDeleteError(
        this ILogger logger,
        Exception exception,
        Guid imageId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for user {UserId}")]
    public static partial void LogImageRateLimitExceeded(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Warning,
        Message = "Hotlinking attempt by user {UserId} from referer {Referer}")]
    public static partial void LogImageHotlinkReferer(
        this ILogger logger,
        string userId,
        string referer);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Warning,
        Message = "Hotlinking attempt by user {UserId} from origin {Origin}")]
    public static partial void LogImageHotlinkOrigin(
        this ILogger logger,
        string userId,
        string origin);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Warning,
        Message = "Image not found: {ImageId} by user {UserId}")]
    public static partial void LogImageNotFound(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 8,
        Level = LogLevel.Warning,
        Message = "File not found for image {ImageId} by user {UserId}")]
    public static partial void LogImageFileNotFound(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 9,
        Level = LogLevel.Warning,
        Message = "Invalid content type for image {ImageId} by user {UserId}")]
    public static partial void LogImageInvalidContentType(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 10,
        Level = LogLevel.Information,
        Message = "Image {ImageId} served to owner {UserId}")]
    public static partial void LogImageServedToOwner(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 11,
        Level = LogLevel.Information,
        Message = "Image {ImageId} served to user {UserId} via box/item access")]
    public static partial void LogImageServedToUserViaAccess(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 12,
        Level = LogLevel.Warning,
        Message = "Forbidden image access attempt: {ImageId} by user {UserId}")]
    public static partial void LogImageForbiddenAccess(
        this ILogger logger,
        Guid imageId,
        string userId);
}
