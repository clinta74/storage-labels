using StorageLabelsApi.DataLayer.Models;

namespace StorageLabelsApi.Services;

/// <summary>
/// Result of an encryption operation
/// </summary>
public record EncryptionResult(
    byte[] EncryptedData,
    byte[] InitializationVector,
    byte[] AuthenticationTag,
    int EncryptionKeyId
);

/// <summary>
/// Service for encrypting and decrypting image files with key versioning support
/// </summary>
public interface IImageEncryptionService
{
    /// <summary>
    /// Encrypt a stream of data using the currently active encryption key
    /// </summary>
    /// <param name="inputStream">Input data to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encryption result with encrypted data, IV, tag, and key ID</returns>
    Task<EncryptionResult> EncryptAsync(Stream inputStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt encrypted data using the specified encryption key
    /// </summary>
    /// <param name="encryptedData">The encrypted data</param>
    /// <param name="keyId">The ID of the encryption key to use</param>
    /// <param name="iv">The initialization vector</param>
    /// <param name="authTag">The authentication tag (for AES-GCM)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted data as a memory stream</returns>
    Task<MemoryStream> DecryptAsync(
        byte[] encryptedData,
        int kid,
        byte[] iv,
        byte[] authTag,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrypt an image based on its metadata
    /// </summary>
    /// <param name="metadata">Image metadata containing encryption information</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decrypted image data as a stream</returns>
    Task<Stream> DecryptImageAsync(ImageMetadata metadata, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the currently active encryption key
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The active encryption key or null if none exists</returns>
    Task<EncryptionKey?> GetActiveKeyAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new encryption key
    /// </summary>
    /// <param name="description">Optional description</param>
    /// <param name="createdBy">User ID who created the key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created encryption key</returns>
    Task<EncryptionKey> CreateKeyAsync(
        string? description,
        string? createdBy,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a key (make it the current key for new encryptions)
    /// </summary>
    /// <param name="kid">The key ID to activate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false if key not found</returns>
    Task<bool> ActivateKeyAsync(int kid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retire a key (disable it for new encryptions but keep for decryption)
    /// </summary>
    /// <param name="kid">The key ID to retire</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false if key not found</returns>
    Task<bool> RetireKeyAsync(int kid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Encrypt an existing unencrypted image file with a specific key
    /// </summary>
    /// <param name="metadata">Image metadata for the unencrypted image</param>
    /// <param name="keyId">ID of the key to use for encryption</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated metadata with encryption information</returns>
    Task<ImageMetadata> EncryptExistingImageAsync(
        ImageMetadata metadata,
        int kid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Re-encrypt an image with a different key
    /// </summary>
    /// <param name="metadata">Image metadata</param>
    /// <param name="newKeyId">ID of the new key to use</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Updated metadata</returns>
    Task<ImageMetadata> ReEncryptImageAsync(
        ImageMetadata metadata,
        int newKid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get statistics about encryption key usage
    /// </summary>
    /// <param name="keyId">The key ID to get stats for</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Statistics including image count, total size, etc.</returns>
    Task<EncryptionKeyStats> GetKeyStatsAsync(int kid, CancellationToken cancellationToken = default);
}

/// <summary>
/// Statistics about an encryption key's usage
/// </summary>
public record EncryptionKeyStats(
    int Kid,
    int Version,
    EncryptionKeyStatus Status,
    int ImageCount,
    long TotalSizeBytes,
    DateTime CreatedAt,
    DateTime? ActivatedAt,
    DateTime? RetiredAt
);

