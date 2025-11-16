using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Services;

/// <summary>
/// Options for key rotation
/// </summary>
public record RotationOptions(
    int FromKeyId,
    int ToKeyId,
    int BatchSize = 100,
    string? InitiatedBy = null,
    bool IsAutomatic = false
);

/// <summary>
/// Progress information for an ongoing rotation
/// </summary>
public record RotationProgress(
    Guid RotationId,
    RotationStatus Status,
    int TotalImages,
    int ProcessedImages,
    int FailedImages,
    double PercentComplete,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage
);

/// <summary>
/// Service for rotating encryption keys
/// </summary>
public interface IKeyRotationService
{
    /// <summary>
    /// Start a key rotation operation
    /// </summary>
    /// <param name="options">Rotation options</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rotation tracking entity</returns>
    Task<EncryptionKeyRotation> StartRotationAsync(
        RotationOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the progress of a rotation operation
    /// </summary>
    /// <param name="rotationId">Rotation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Progress information</returns>
    Task<RotationProgress?> GetRotationProgressAsync(
        Guid rotationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all rotation operations (optionally filtered by status)
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of rotation operations</returns>
    Task<List<EncryptionKeyRotation>> GetRotationsAsync(
        RotationStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel an in-progress rotation
    /// </summary>
    /// <param name="rotationId">Rotation ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if cancelled successfully</returns>
    Task<bool> CancelRotationAsync(
        Guid rotationId,
        CancellationToken cancellationToken = default);
}
