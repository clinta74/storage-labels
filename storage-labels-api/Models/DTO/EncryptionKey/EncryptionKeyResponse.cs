using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

/// <summary>
/// Response DTO for encryption key (excludes sensitive data and navigation properties)
/// </summary>
public record EncryptionKeyResponse
{
    public int Kid { get; init; }
    public int Version { get; init; }
    public EncryptionKeyStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ActivatedAt { get; init; }
    public DateTime? RetiredAt { get; init; }
    public DateTime? DeprecatedAt { get; init; }
    public string? Description { get; init; }
    public string? CreatedBy { get; init; }
    public required string Algorithm { get; init; }

    public static EncryptionKeyResponse FromEntity(DataLayer.Models.EncryptionKey key)
    {
        return new EncryptionKeyResponse
        {
            Kid = key.Kid,
            Version = key.Version,
            Status = key.Status,
            CreatedAt = key.CreatedAt,
            ActivatedAt = key.ActivatedAt,
            RetiredAt = key.RetiredAt,
            DeprecatedAt = key.DeprecatedAt,
            Description = key.Description,
            CreatedBy = key.CreatedBy,
            Algorithm = key.Algorithm
        };
    }
}
