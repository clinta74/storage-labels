using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Image {ImageId} uploaded by user {UserId} with filename {FileName}")]
    public static partial void LogImageUploaded(
        this ILogger logger,
        Guid imageId,
        string userId,
        string fileName);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Image {ImageId} deleted by user {UserId}")]
    public static partial void LogImageDeleted(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Failed to delete image file {ImageId}")]
    public static partial void LogImageDeleteError(
        this ILogger logger,
        Exception exception,
        Guid imageId);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "Rate limit exceeded for user {UserId}")]
    public static partial void LogImageRateLimitExceeded(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Warning,
        Message = "Hotlinking attempt by user {UserId} from referer {Referer}")]
    public static partial void LogImageHotlinkReferer(
        this ILogger logger,
        string userId,
        string referer);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Hotlinking attempt by user {UserId} from origin {Origin}")]
    public static partial void LogImageHotlinkOrigin(
        this ILogger logger,
        string userId,
        string origin);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Warning,
        Message = "Image not found: {ImageId} by user {UserId}")]
    public static partial void LogImageNotFound(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Warning,
        Message = "File not found for image {ImageId} by user {UserId}")]
    public static partial void LogImageFileNotFound(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1008,
        Level = LogLevel.Warning,
        Message = "Invalid content type for image {ImageId} by user {UserId}")]
    public static partial void LogImageInvalidContentType(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1009,
        Level = LogLevel.Information,
        Message = "Image {ImageId} served to owner {UserId}")]
    public static partial void LogImageServedToOwner(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1010,
        Level = LogLevel.Information,
        Message = "Image {ImageId} served to user {UserId} via box/item access")]
    public static partial void LogImageServedToUserViaAccess(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1011,
        Level = LogLevel.Warning,
        Message = "Forbidden image access attempt: {ImageId} by user {UserId}")]
    public static partial void LogImageForbiddenAccess(
        this ILogger logger,
        Guid imageId,
        string userId);

    [LoggerMessage(
        EventId = 1012,
        Level = LogLevel.Warning,
        Message = "No active encryption key found. Saving image unencrypted")]
    public static partial void NoActiveEncryptionKeyFound(
        this ILogger logger);

    [LoggerMessage(
        EventId = 1013,
        Level = LogLevel.Information,
        Message = "Image {ImageId} encrypted with key {Kid}")]
    public static partial void ImageEncryptedWithKey(
        this ILogger logger,
        Guid imageId,
        int kid);

    [LoggerMessage(
        EventId = 1014,
        Level = LogLevel.Error,
        Message = "Failed to encrypt image {ImageId}. Saving unencrypted")]
    public static partial void ImageEncryptionFailed(
        this ILogger logger,
        Exception ex,
        Guid imageId);

    [LoggerMessage(
        EventId = 1015,
        Level = LogLevel.Error,
        Message = "Failed to decrypt image {ImageId} for user {UserId}")]
    public static partial void ImageDecryptionFailed(
        this ILogger logger,
        Exception ex,
        Guid imageId,
        string userId);
}
