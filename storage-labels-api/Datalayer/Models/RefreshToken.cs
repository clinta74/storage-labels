using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorageLabelsApi.Datalayer.Models;

/// <summary>
/// Server-side representation of a single-use refresh token.
/// </summary>
public class RefreshToken
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public string UserId { get; set; } = default!;

    /// <summary>
    /// Hashed representation of the refresh token (Base64 encoded SHA-256).
    /// </summary>
    [Required]
    [MaxLength(172)] // Base64 encoded SHA-256 hash length
    public string TokenHash { get; set; } = default!;

    /// <summary>
    /// Optional token issued from which this was rotated.
    /// </summary>
    public Guid? ReplacedByTokenId { get; set; }

    public Guid? ParentTokenId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? CreatedByIp { get; set; }

    public string? UserAgent { get; set; }

    /// <summary>
    /// Indicates whether the token lifetime is extended (Remember Me).
    /// </summary>
    public bool IsPersistent { get; set; }

    [ForeignKey(nameof(UserId))]
    public ApplicationUser User { get; set; } = default!;
}
