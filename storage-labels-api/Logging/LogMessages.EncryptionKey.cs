using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    // CreateEncryptionKeyHandler
    [LoggerMessage(
        EventId = 5000,
        Level = LogLevel.Information,
        Message = "User {UserId} created encryption key {Kid} (v{Version})")]
    public static partial void EncryptionKeyCreated(
        this ILogger logger,
        string userId,
        int kid,
        int version);

    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Warning,
        Message = "Failed to create encryption key {Kid}")]
    public static partial void EncryptionKeyCreationFailed(
        this ILogger logger,
        Exception ex,
        int kid);

    // GetEncryptionKeyStatsHandler
    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "Encryption key {Kid} not found for stats retrieval")]
    public static partial void EncryptionKeyStatsNotFound(
        this ILogger logger,
        int kid);

    // ActivateEncryptionKeyHandler
    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Warning,
        Message = "User {UserId} attempted to activate non-existent key {Kid}")]
    public static partial void ActivateNonExistentKey(
        this ILogger logger,
        string userId,
        int kid);

    [LoggerMessage(
        EventId = 5004,
        Level = LogLevel.Information,
        Message = "User {UserId} activated encryption key {Kid}")]
    public static partial void EncryptionKeyActivated(
        this ILogger logger,
        string userId,
        int kid);

    [LoggerMessage(
        EventId = 5005,
        Level = LogLevel.Information,
        Message = "Auto-rotation started {RotationId}: {FromKid} -> {ToKid}")]
    public static partial void AutoRotationStarted(
        this ILogger logger,
        Guid rotationId,
        int fromKid,
        int toKid);

    [LoggerMessage(
        EventId = 5006,
        Level = LogLevel.Error,
        Message = "Failed to start auto-rotation for key activation")]
    public static partial void AutoRotationFailed(
        this ILogger logger,
        Exception ex);

    // RetireEncryptionKeyHandler
    [LoggerMessage(
        EventId = 5007,
        Level = LogLevel.Warning,
        Message = "User {UserId} attempted to retire non-existent key {Kid}")]
    public static partial void RetireNonExistentKey(
        this ILogger logger,
        string userId,
        int kid);

    [LoggerMessage(
        EventId = 5008,
        Level = LogLevel.Information,
        Message = "User {UserId} retired encryption key {Kid}")]
    public static partial void EncryptionKeyRetired(
        this ILogger logger,
        string userId,
        int kid);

    // StartKeyRotationHandler
    [LoggerMessage(
        EventId = 5009,
        Level = LogLevel.Information,
        Message = "User {UserId} started manual rotation {RotationId}: {FromKid} -> {ToKid}")]
    public static partial void ManualRotationStarted(
        this ILogger logger,
        string userId,
        Guid rotationId,
        int? fromKid,
        int toKid);

    [LoggerMessage(
        EventId = 5010,
        Level = LogLevel.Error,
        Message = "Failed to start key rotation")]
    public static partial void KeyRotationStartFailed(
        this ILogger logger,
        Exception ex);

    // CancelRotationHandler
    [LoggerMessage(
        EventId = 5011,
        Level = LogLevel.Information,
        Message = "User {UserId} cancelled rotation {RotationId}")]
    public static partial void RotationCancelled(
        this ILogger logger,
        string userId,
        Guid rotationId);

    [LoggerMessage(
        EventId = 5012,
        Level = LogLevel.Warning,
        Message = "User {UserId} attempted to cancel non-existent rotation {RotationId}")]
    public static partial void CancelNonExistentRotation(
        this ILogger logger,
        string userId,
        Guid rotationId);

    // ImageEncryptionService
    [LoggerMessage(
        EventId = 5013,
        Level = LogLevel.Information,
        Message = "Encrypted {Size} bytes using key {Kid} (v{Version})")]
    public static partial void BytesEncrypted(
        this ILogger logger,
        int size,
        int kid,
        int version);

    [LoggerMessage(
        EventId = 5014,
        Level = LogLevel.Warning,
        Message = "Using deprecated key {Kid} (v{Version}) for decryption")]
    public static partial void UsingDeprecatedKey(
        this ILogger logger,
        int kid,
        int version);

    [LoggerMessage(
        EventId = 5015,
        Level = LogLevel.Error,
        Message = "Failed to decrypt data with key {Kid} (v{Version}). Data may be corrupted or tampered")]
    public static partial void DecryptionFailed(
        this ILogger logger,
        Exception ex,
        int kid,
        int version);

    // KeyRotationService
    [LoggerMessage(
        EventId = 5016,
        Level = LogLevel.Information,
        Message = "Started unencrypted image migration {RotationId}: unencrypted -> {ToKid} ({TotalImages} images, batch size {BatchSize})")]
    public static partial void UnencryptedImageMigrationStarted(
        this ILogger logger,
        Guid rotationId,
        int toKid,
        int totalImages,
        int batchSize);

    [LoggerMessage(
        EventId = 5017,
        Level = LogLevel.Information,
        Message = "Started key rotation {RotationId}: {FromKid} -> {ToKid} ({TotalImages} images, batch size {BatchSize})")]
    public static partial void KeyRotationStarted(
        this ILogger logger,
        Guid rotationId,
        int fromKid,
        int toKid,
        int totalImages,
        int batchSize);

    [LoggerMessage(
        EventId = 5018,
        Level = LogLevel.Error,
        Message = "Rotation {RotationId} not found")]
    public static partial void RotationNotFound(
        this ILogger logger,
        Guid rotationId);

    [LoggerMessage(
        EventId = 5019,
        Level = LogLevel.Information,
        Message = "Beginning {Operation} {RotationId}: {FromKey} -> {ToKey}")]
    public static partial void RotationOperationBeginning(
        this ILogger logger,
        string operation,
        Guid rotationId,
        string fromKey,
        int toKey);

    [LoggerMessage(
        EventId = 5020,
        Level = LogLevel.Information,
        Message = "{Operation} {RotationId} was cancelled")]
    public static partial void RotationOperationCancelled(
        this ILogger logger,
        string operation,
        Guid rotationId);

    [LoggerMessage(
        EventId = 5021,
        Level = LogLevel.Information,
        Message = "Processing batch {BatchNumber} of {Operation} {RotationId}: {Count} images")]
    public static partial void RotationBatchProcessing(
        this ILogger logger,
        int batchNumber,
        string operation,
        Guid rotationId,
        int count);

    [LoggerMessage(
        EventId = 5022,
        Level = LogLevel.Error,
        Message = "Failed to {Operation} image {ImageId}: {Error}")]
    public static partial void ImageRotationFailed(
        this ILogger logger,
        string operation,
        Guid imageId,
        string error);

    [LoggerMessage(
        EventId = 5023,
        Level = LogLevel.Information,
        Message = "Batch {BatchNumber} complete: {Processed}/{Total} processed, {Failed} failed")]
    public static partial void RotationBatchComplete(
        this ILogger logger,
        int batchNumber,
        int processed,
        int total,
        int failed);

    [LoggerMessage(
        EventId = 5024,
        Level = LogLevel.Information,
        Message = "{Operation} {RotationId} complete: Status={Status}, Processed={Processed}/{Total}, Failed={Failed}")]
    public static partial void RotationComplete(
        this ILogger logger,
        string operation,
        Guid rotationId,
        string status,
        int processed,
        int total,
        int failed);

    [LoggerMessage(
        EventId = 5025,
        Level = LogLevel.Error,
        Message = "Fatal error during rotation {RotationId}")]
    public static partial void RotationFatalError(
        this ILogger logger,
        Exception ex,
        Guid rotationId);

    [LoggerMessage(
        EventId = 5026,
        Level = LogLevel.Error,
        Message = "Failed to update rotation {RotationId} status after error")]
    public static partial void RotationStatusUpdateFailed(
        this ILogger logger,
        Exception ex,
        Guid rotationId);

    [LoggerMessage(
        EventId = 5027,
        Level = LogLevel.Information,
        Message = "Cancelled rotation {RotationId}")]
    public static partial void RotationCancelledInService(
        this ILogger logger,
        Guid rotationId);

    [LoggerMessage(
        EventId = 5028,
        Level = LogLevel.Warning,
        Message = "Failed to create encryption key: {Description}")]
    public static partial void EncryptionKeyCreationWarning(
        this ILogger logger,
        Exception ex,
        string description);

    [LoggerMessage(
        EventId = 5029,
        Level = LogLevel.Information,
        Message = "User {UserId} created encryption key {Kid} (v{Version}) - {Description}")]
    public static partial void EncryptionKeyCreatedWithDescription(
        this ILogger logger,
        string userId,
        int kid,
        int version,
        string description);

    [LoggerMessage(
        EventId = 5030,
        Level = LogLevel.Information,
        Message = "User {UserId} activated encryption key {Kid} (v{Version}), retired {RetiredCount} keys")]
    public static partial void EncryptionKeyActivatedWithRetired(
        this ILogger logger,
        string userId,
        int kid,
        int version,
        int retiredCount);

    [LoggerMessage(
        EventId = 5031,
        Level = LogLevel.Error,
        Message = "Failed to activate encryption key {Kid}")]
    public static partial void EncryptionKeyActivationFailed(
        this ILogger logger,
        Exception ex,
        int kid);

    [LoggerMessage(
        EventId = 5032,
        Level = LogLevel.Information,
        Message = "User {UserId} retired encryption key {Kid} (v{Version})")]
    public static partial void EncryptionKeyRetiredWithVersion(
        this ILogger logger,
        string userId,
        int kid,
        int version);

    [LoggerMessage(
        EventId = 5033,
        Level = LogLevel.Information,
        Message = "Encrypted previously unencrypted image {ImageId} with key {Kid}")]
    public static partial void UnencryptedImageEncrypted(
        this ILogger logger,
        Guid imageId,
        int kid);

    [LoggerMessage(
        EventId = 5034,
        Level = LogLevel.Information,
        Message = "Re-encrypted image {ImageId} from key {FromKid} to {ToKid}")]
    public static partial void ImageReencrypted(
        this ILogger logger,
        Guid imageId,
        int fromKid,
        int toKid);
}
