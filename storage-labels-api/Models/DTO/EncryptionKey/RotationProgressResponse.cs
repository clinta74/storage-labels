using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

/// <summary>
/// Progress information for an active rotation operation
/// </summary>
public record RotationProgressResponse(
    Guid RotationId,
    RotationStatus Status,
    int TotalImages,
    int ProcessedImages,
    int FailedImages,
    double PercentComplete,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage)
{
    public RotationProgressResponse(StorageLabelsApi.Services.RotationProgress progress) : this(
        progress.RotationId,
        progress.Status,
        progress.TotalImages,
        progress.ProcessedImages,
        progress.FailedImages,
        progress.PercentComplete,
        progress.StartedAt,
        progress.CompletedAt,
        progress.ErrorMessage)
    {
    }
}
