using StorageLabelsApi.DataLayer.Models;
using StorageLabelsApi.Services;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

/// <summary>
/// Statistics about an encryption key's usage
/// </summary>
public record EncryptionKeyStatsResponse(
    int Kid,
    int Version,
    EncryptionKeyStatus Status,
    int ImageCount,
    long TotalSizeBytes,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? RetiredAt)
{
    public EncryptionKeyStatsResponse(StorageLabelsApi.Services.EncryptionKeyStats stats) : this(
        stats.Kid,
        stats.Version,
        stats.Status,
        stats.ImageCount,
        stats.TotalSizeBytes,
        stats.CreatedAt,
        stats.ActivatedAt,
        stats.RetiredAt)
    {
    }
}
