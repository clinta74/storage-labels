using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

/// <summary>
/// Response DTO for encryption key (excludes sensitive data and navigation properties)
/// </summary>
public record EncryptionKeyResponse(
    int Kid,
    int Version,
    EncryptionKeyStatus Status,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? RetiredAt,
    DateTime? DeprecatedAt,
    string? Description,
    string? CreatedBy,
    string Algorithm
)
{
    public EncryptionKeyResponse(DataLayer.Models.EncryptionKey key) : this(
        key.Kid,
        key.Version,
        key.Status,
        key.CreatedAt,
        key.ActivatedAt,
        key.RetiredAt,
        key.DeprecatedAt,
        key.Description,
        key.CreatedBy,
        key.Algorithm
    )
    { }
}
