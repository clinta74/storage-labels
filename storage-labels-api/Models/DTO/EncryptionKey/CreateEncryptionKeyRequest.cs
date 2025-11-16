using System.ComponentModel.DataAnnotations;

namespace StorageLabelsApi.Models.DTO.EncryptionKey;

public record CreateEncryptionKeyRequest
{
    [StringLength(500)]
    public string? Description { get; init; }
}
