using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

public class ImageMetadata
{
    public Guid ImageId { get; set; }
    public required string UserId { get; set; }
    public required string FileName { get; set; }
    public required string ContentType { get; set; }
    public required string StoragePath { get; set; }
    public DateTime UploadedAt { get; set; }
    public long SizeInBytes { get; set; }
    
    /// <summary>
    /// Whether this image is encrypted on disk
    /// </summary>
    public bool IsEncrypted { get; set; } = false;
    
    /// <summary>
    /// Foreign key to the encryption key used (if encrypted)
    /// References EncryptionKey.Kid
    /// </summary>
    public int? EncryptionKeyId { get; set; }
    
    /// <summary>
    /// Navigation property to the encryption key
    /// </summary>
    [ForeignKey(nameof(EncryptionKeyId))]
    public virtual EncryptionKey? EncryptionKey { get; set; }
    
    /// <summary>
    /// Initialization Vector (IV) used for encryption (unique per image)
    /// AES requires 16 bytes (128 bits) IV
    /// </summary>
    public byte[]? InitializationVector { get; set; }
    
    /// <summary>
    /// Authentication tag for AES-GCM (provides integrity verification)
    /// </summary>
    public byte[]? AuthenticationTag { get; set; }
    
    public virtual ICollection<Box> ReferencedByBoxes { get; set; } = new List<Box>();
    public virtual ICollection<Item> ReferencedByItems { get; set; } = new List<Item>();
}
