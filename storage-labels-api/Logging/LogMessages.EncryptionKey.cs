using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    // CreateEncryptionKeyHandler
    [LoggerMessage(Message = "User {userId} created encryption key {kid} (v{version})", Level = LogLevel.Information)]
    public static partial void EncryptionKeyCreated(this ILogger logger, string userId, int kid, int version);

    [LoggerMessage(Message = "Failed to create encryption key {kid}", Level = LogLevel.Warning)]
    public static partial void EncryptionKeyCreationFailed(this ILogger logger, Exception ex, int kid);

    // GetEncryptionKeyStatsHandler
    [LoggerMessage(Message = "Encryption key {kid} not found for stats retrieval", Level = LogLevel.Warning)]
    public static partial void EncryptionKeyStatsNotFound(this ILogger logger, int kid);

    // ActivateEncryptionKeyHandler
    [LoggerMessage(Message = "User {userId} attempted to activate non-existent key {kid}", Level = LogLevel.Warning)]
    public static partial void ActivateNonExistentKey(this ILogger logger, string userId, int kid);

    [LoggerMessage(Message = "User {userId} activated encryption key {kid}", Level = LogLevel.Information)]
    public static partial void EncryptionKeyActivated(this ILogger logger, string userId, int kid);

    [LoggerMessage(Message = "Auto-rotation started {rotationId}: {fromKid} -> {toKid}", Level = LogLevel.Information)]
    public static partial void AutoRotationStarted(this ILogger logger, Guid rotationId, int fromKid, int toKid);

    [LoggerMessage(Message = "Failed to start auto-rotation for key activation", Level = LogLevel.Error)]
    public static partial void AutoRotationFailed(this ILogger logger, Exception ex);

    // RetireEncryptionKeyHandler
    [LoggerMessage(Message = "User {userId} attempted to retire non-existent key {kid}", Level = LogLevel.Warning)]
    public static partial void RetireNonExistentKey(this ILogger logger, string userId, int kid);

    [LoggerMessage(Message = "User {userId} retired encryption key {kid}", Level = LogLevel.Information)]
    public static partial void EncryptionKeyRetired(this ILogger logger, string userId, int kid);

    // StartKeyRotationHandler
    [LoggerMessage(Message = "User {userId} started manual rotation {rotationId}: {fromKid} -> {toKid}", Level = LogLevel.Information)]
    public static partial void ManualRotationStarted(this ILogger logger, string userId, Guid rotationId, int fromKid, int toKid);

    [LoggerMessage(Message = "Failed to start key rotation", Level = LogLevel.Error)]
    public static partial void KeyRotationStartFailed(this ILogger logger, Exception ex);

    // CancelRotationHandler
    [LoggerMessage(Message = "User {userId} cancelled rotation {rotationId}", Level = LogLevel.Information)]
    public static partial void RotationCancelled(this ILogger logger, string userId, Guid rotationId);

    [LoggerMessage(Message = "User {userId} attempted to cancel non-existent rotation {rotationId}", Level = LogLevel.Warning)]
    public static partial void CancelNonExistentRotation(this ILogger logger, string userId, Guid rotationId);
}
