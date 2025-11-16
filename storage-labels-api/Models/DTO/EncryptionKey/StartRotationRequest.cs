using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

public record StartRotationRequest
{
    /// <summary>
    /// Source key ID to rotate from. Null indicates encrypting unencrypted images.
    /// </summary>
    public int? FromKeyId { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public required int ToKeyId { get; init; }

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 100;
}
