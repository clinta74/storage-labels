using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

/// <summary>
/// Status of an encryption key
/// </summary>
public enum EncryptionKeyStatus
{
    /// <summary>
    /// Key has been created but not yet activated
    /// </summary>
    Created,
    
    /// <summary>
    /// Key is active and used for new encryptions
    /// </summary>
    Active,
    
    /// <summary>
    /// Key is deprecated but still available for decryption (being rotated out)
    /// </summary>
    Deprecated,
    
    /// <summary>
    /// Key is retired but still available for decryption
    /// </summary>
    Retired
}

/// <summary>
/// Encryption key entity for managing image encryption keys with versioning
/// </summary>
public class EncryptionKey
{
    /// <summary>
    /// Key ID (kid) - auto-incremented integer primary key
    /// </summary>
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Kid { get; set; }
    
    /// <summary>
    /// Version number for ordering and tracking
    /// </summary>
    public int Version { get; set; }
    
    /// <summary>
    /// The actual encryption key (AES-256 = 32 bytes, base64 encoded)
    /// IMPORTANT: This should be encrypted at rest in production!
    /// </summary>
    [Required]
    public required byte[] KeyMaterial { get; set; }
    
    /// <summary>
    /// Status of the key (Active, Retired, Deprecated)
    /// </summary>
    public EncryptionKeyStatus Status { get; set; } = EncryptionKeyStatus.Active;
    
    /// <summary>
    /// When this key was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When this key was activated (became active)
    /// </summary>
    public DateTime? ActivatedAt { get; set; }
    
    /// <summary>
    /// When this key was retired (no longer used for new encryptions)
    /// </summary>
    public DateTime? RetiredAt { get; set; }
    
    /// <summary>
    /// When this key was deprecated (marked for deletion)
    /// </summary>
    public DateTime? DeprecatedAt { get; set; }
    
    /// <summary>
    /// Optional description for this key
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }
    
    /// <summary>
    /// Who created this key (UserId)
    /// </summary>
    [MaxLength(250)]
    public string? CreatedBy { get; set; }
    
    /// <summary>
    /// Algorithm used (e.g., "AES-256-GCM")
    /// </summary>
    [MaxLength(50)]
    public string Algorithm { get; set; } = "AES-256-GCM";
    
    /// <summary>
    /// Images encrypted with this key
    /// </summary>
    public virtual ICollection<ImageMetadata> Images { get; set; } = new List<ImageMetadata>();
}
