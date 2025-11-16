using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

public record StartRotationRequest
{
    [Required]
    [Range(1, int.MaxValue)]
    public required int FromKeyId { get; init; }

    [Required]
    [Range(1, int.MaxValue)]
    public required int ToKeyId { get; init; }

    [Range(1, 1000)]
    public int BatchSize { get; init; } = 100;
}
