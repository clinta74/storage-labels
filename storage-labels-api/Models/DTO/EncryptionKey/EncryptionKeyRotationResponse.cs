using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

/// <summary>
/// Information about an encryption key rotation operation
/// </summary>
public record EncryptionKeyRotationResponse(
    Guid Id,
    int? FromKeyId,
    int ToKeyId,
    RotationStatus Status,
    DateTime StartedAt,
    DateTime? CompletedAt,
    int TotalImages,
    int ProcessedImages,
    int FailedImages,
    string? ErrorMessage)
{
    public EncryptionKeyRotationResponse(EncryptionKeyRotation rotation) : this(
        rotation.Id,
        rotation.FromKeyId,
        rotation.ToKeyId,
        rotation.Status,
        rotation.StartedAt,
        rotation.CompletedAt,
        rotation.TotalImages,
        rotation.ProcessedImages,
        rotation.FailedImages,
        rotation.ErrorMessage)
    {
    }
}
