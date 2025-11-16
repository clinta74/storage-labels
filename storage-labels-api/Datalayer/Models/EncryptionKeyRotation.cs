using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.DataLayer.Models;

/// <summary>
/// Status of a key rotation operation
/// </summary>
public enum RotationStatus
{
    /// <summary>
    /// Rotation is in progress
    /// </summary>
    InProgress,
    
    /// <summary>
    /// Rotation completed successfully
    /// </summary>
    Completed,
    
    /// <summary>
    /// Rotation failed with errors
    /// </summary>
    Failed,
    
    /// <summary>
    /// Rotation was cancelled
    /// </summary>
    Cancelled
}

/// <summary>
/// Tracks encryption key rotation operations
/// </summary>
[Table("encryptionkeyrotations")]
public class EncryptionKeyRotation
{
    /// <summary>
    /// Unique identifier for the rotation operation
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID of the key being rotated FROM (references EncryptionKey.Kid)
    /// Null indicates migration from unencrypted images
    /// </summary>
    public int? FromKeyId { get; set; }

    /// <summary>
    /// Navigation property to the source key
    /// </summary>
    [ForeignKey(nameof(FromKeyId))]
    public virtual EncryptionKey? FromKey { get; set; }

    /// <summary>
    /// ID of the key being rotated TO (references EncryptionKey.Kid)
    /// </summary>
    public int ToKeyId { get; set; }

    /// <summary>
    /// Navigation property to the target key
    /// </summary>
    [ForeignKey(nameof(ToKeyId))]
    public virtual EncryptionKey? ToKey { get; set; }

    /// <summary>
    /// Current status of the rotation
    /// </summary>
    public RotationStatus Status { get; set; }

    /// <summary>
    /// Total number of images to rotate
    /// </summary>
    public int TotalImages { get; set; }

    /// <summary>
    /// Number of images successfully rotated
    /// </summary>
    public int ProcessedImages { get; set; }

    /// <summary>
    /// Number of images that failed to rotate
    /// </summary>
    public int FailedImages { get; set; }

    /// <summary>
    /// Batch size for processing
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    /// When the rotation started
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the rotation completed or failed
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// User who initiated the rotation
    /// </summary>
    [MaxLength(250)]
    public string? InitiatedBy { get; set; }

    /// <summary>
    /// Error message if rotation failed
    /// </summary>
    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Whether this rotation was triggered automatically
    /// </summary>
    public bool IsAutomatic { get; set; }
}
