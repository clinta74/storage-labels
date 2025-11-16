using Microsoft.EntityFrameworkCore;
using StorageLabelsApi.Datalayer;
using StorageLabelsApi.DataLayer.Models;
using System.Security.Cryptography;

namespace StorageLabelsApi.Services;

/// <summary>
/// Implementation of image encryption service using AES-256-GCM
/// </summary>
public class ImageEncryptionService : IImageEncryptionService
{
    private readonly StorageLabelsDbContext _context;
    private readonly ILogger<ImageEncryptionService> _logger;
    private const int KeySizeBytes = 32; // AES-256
    private const int IVSizeBytes = 12; // AES-GCM recommended IV size
    private const int TagSizeBytes = 16; // AES-GCM authentication tag size
    private const string Algorithm = "AES-256-GCM";

    public ImageEncryptionService(
        StorageLabelsDbContext context,
        ILogger<ImageEncryptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EncryptionResult> EncryptAsync(
        Stream inputStream,
        CancellationToken cancellationToken = default)
    {
        var activeKey = await GetActiveKeyAsync(cancellationToken);
        if (activeKey == null)
        {
            throw new InvalidOperationException("No active encryption key found. Please create and activate an encryption key first.");
        }

        // Read input stream into memory
        using var inputMemory = new MemoryStream();
        await inputStream.CopyToAsync(inputMemory, cancellationToken);
        var plaintext = inputMemory.ToArray();

        // Generate random IV (nonce) for this encryption
        var iv = new byte[IVSizeBytes];
        RandomNumberGenerator.Fill(iv);

        // Prepare buffers
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        // Encrypt using AES-GCM
        using var aesGcm = new AesGcm(activeKey.KeyMaterial, TagSizeBytes);
        aesGcm.Encrypt(iv, plaintext, ciphertext, tag);

        _logger.LogInformation(
            "Encrypted {Size} bytes using key {Kid} (v{Version})",
            plaintext.Length,
            activeKey.Kid,
            activeKey.Version);

        return new EncryptionResult(
            EncryptedData: ciphertext,
            InitializationVector: iv,
            AuthenticationTag: tag,
            EncryptionKeyId: activeKey.Kid
        );
    }

    public async Task<MemoryStream> DecryptAsync(
        byte[] encryptedData,
        int kid,
        byte[] iv,
        byte[] authTag,
        CancellationToken cancellationToken = default)
    {
        var key = await _context.EncryptionKeys
            .AsNoTracking()
            .FirstOrDefaultAsync(k => k.Kid == kid, cancellationToken);

        if (key == null)
        {
            throw new InvalidOperationException($"Encryption key {kid} not found");
        }

        if (key.Status == EncryptionKeyStatus.Deprecated)
        {
            _logger.LogWarning(
                "Using deprecated key {Kid} (v{Version}) for decryption",
                key.Kid,
                key.Version);
        }

        // Prepare plaintext buffer
        var plaintext = new byte[encryptedData.Length];

        // Decrypt using AES-GCM
        try
        {
            using var aesGcm = new AesGcm(key.KeyMaterial, TagSizeBytes);
            aesGcm.Decrypt(iv, encryptedData, authTag, plaintext);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(
                ex,
                "Failed to decrypt data with key {Kid} (v{Version}). Data may be corrupted or tampered.",
                key.Kid,
                key.Version);
            throw new InvalidOperationException("Failed to decrypt image. Data integrity check failed.", ex);
        }

        _logger.LogDebug(
            "Decrypted {Size} bytes using key {Kid} (v{Version})",
            encryptedData.Length,
            key.Kid,
            key.Version);

        return new MemoryStream(plaintext);
    }

    public async Task<Stream> DecryptImageAsync(
        ImageMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        if (!metadata.IsEncrypted)
        {
            throw new InvalidOperationException("Image is not encrypted");
        }

        if (metadata.EncryptionKeyId == null || metadata.InitializationVector == null || metadata.AuthenticationTag == null)
        {
            throw new InvalidOperationException("Image encryption metadata is incomplete");
        }

        // Use the StoragePath from metadata (already contains the full path)
        if (!File.Exists(metadata.StoragePath))
        {
            throw new FileNotFoundException($"Image file not found: {metadata.StoragePath}");
        }

        var encryptedData = await File.ReadAllBytesAsync(metadata.StoragePath, cancellationToken);

        return await DecryptAsync(
            encryptedData,
            metadata.EncryptionKeyId.Value,
            metadata.InitializationVector,
            metadata.AuthenticationTag,
            cancellationToken);
    }

    public async Task<EncryptionKey?> GetActiveKeyAsync(CancellationToken cancellationToken = default)
    {
        return await _context.EncryptionKeys
            .Where(k => k.Status == EncryptionKeyStatus.Active)
            .OrderByDescending(k => k.Version)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<EncryptionKey> CreateKeyAsync(
        string? description,
        string? createdBy,
        CancellationToken cancellationToken = default)
    {
        // Generate cryptographically secure random key material
        var keyMaterial = new byte[KeySizeBytes];
        RandomNumberGenerator.Fill(keyMaterial);

        // Determine version (highest current version + 1)
        var maxVersion = await _context.EncryptionKeys
            .MaxAsync(k => (int?)k.Version, cancellationToken) ?? 0;

        var newKey = new EncryptionKey
        {
            // Kid will be auto-generated by the database
            Version = maxVersion + 1,
            KeyMaterial = keyMaterial,
            Status = EncryptionKeyStatus.Created, // Start as created, must be explicitly activated
            Algorithm = Algorithm,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.EncryptionKeys.Add(newKey);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Created new encryption key {Kid} (v{Version}) by {User}",
            newKey.Kid,
            newKey.Version,
            createdBy ?? "system");

        return newKey;
    }

    public async Task<bool> ActivateKeyAsync(int kid, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Find the key to activate
            var keyToActivate = await _context.EncryptionKeys
                .FirstOrDefaultAsync(k => k.Kid == kid, cancellationToken);

            if (keyToActivate == null)
            {
                return false;
            }

            // Retire all currently active keys
            var activeKeys = await _context.EncryptionKeys
                .Where(k => k.Status == EncryptionKeyStatus.Active)
                .ToListAsync(cancellationToken);

            foreach (var activeKey in activeKeys)
            {
                activeKey.Status = EncryptionKeyStatus.Retired;
                activeKey.RetiredAt = DateTime.UtcNow;
            }

            // Activate the new key
            keyToActivate.Status = EncryptionKeyStatus.Active;
            keyToActivate.ActivatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Activated encryption key {Kid} (v{Version}), retired {Count} previous key(s)",
                keyToActivate.Kid,
                keyToActivate.Version,
                activeKeys.Count);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to activate encryption key {Kid}", kid);
            throw;
        }
    }

    public async Task<bool> RetireKeyAsync(int kid, CancellationToken cancellationToken = default)
    {
        var key = await _context.EncryptionKeys
            .FirstOrDefaultAsync(k => k.Kid == kid, cancellationToken);

        if (key == null)
        {
            return false;
        }

        if (key.Status == EncryptionKeyStatus.Retired)
        {
            return true; // Already retired
        }

        key.Status = EncryptionKeyStatus.Retired;
        key.RetiredAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Retired encryption key {Kid} (v{Version})",
            key.Kid,
            key.Version);

        return true;
    }

    public async Task<ImageMetadata> EncryptExistingImageAsync(
        ImageMetadata metadata,
        int kid,
        CancellationToken cancellationToken = default)
    {
        if (metadata.IsEncrypted)
        {
            throw new InvalidOperationException("Image is already encrypted");
        }

        // Read the unencrypted file
        if (!File.Exists(metadata.StoragePath))
        {
            throw new FileNotFoundException($"Image file not found: {metadata.StoragePath}");
        }

        var unencryptedData = await File.ReadAllBytesAsync(metadata.StoragePath, cancellationToken);

        // Get the target key
        var targetKey = await _context.EncryptionKeys
            .FirstOrDefaultAsync(k => k.Kid == kid, cancellationToken);

        if (targetKey == null)
        {
            throw new InvalidOperationException($"Encryption key {kid} not found");
        }

        // Encrypt the data
        using var inputStream = new MemoryStream(unencryptedData);
        var activeKey = await _context.EncryptionKeys
            .FirstOrDefaultAsync(k => k.Status == EncryptionKeyStatus.Active, cancellationToken);

        if (activeKey == null || activeKey.Kid != kid)
        {
            // Temporarily use the specified key
            activeKey = targetKey;
        }

        var iv = new byte[12];
        RandomNumberGenerator.Fill(iv);

        var authTag = new byte[16];
        var encryptedData = new byte[unencryptedData.Length];

        using var aesGcm = new AesGcm(targetKey.KeyMaterial, authTag.Length);
        aesGcm.Encrypt(iv, unencryptedData, encryptedData, authTag);

        // Write encrypted data back to the same file
        await File.WriteAllBytesAsync(metadata.StoragePath, encryptedData, cancellationToken);

        // Update metadata
        metadata.IsEncrypted = true;
        metadata.EncryptionKeyId = kid;
        metadata.InitializationVector = iv;
        metadata.AuthenticationTag = authTag;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Encrypted previously unencrypted image {ImageId} with key {Kid}",
            metadata.ImageId,
            kid);

        return metadata;
    }

    public async Task<ImageMetadata> ReEncryptImageAsync(
        ImageMetadata metadata,
        int newKid,
        CancellationToken cancellationToken = default)
    {
        // Decrypt with old key
        var decryptedStream = await DecryptImageAsync(metadata, cancellationToken);

        // Encrypt with new key
        var newKey = await _context.EncryptionKeys
            .FirstOrDefaultAsync(k => k.Kid == newKid, cancellationToken);

        if (newKey == null)
        {
            throw new InvalidOperationException($"Target encryption key {newKid} not found");
        }

        var encryptionResult = await EncryptAsync(decryptedStream, cancellationToken);

        // Write encrypted data back to file using the metadata's storage path
        await File.WriteAllBytesAsync(metadata.StoragePath, encryptionResult.EncryptedData, cancellationToken);

        // Update metadata
        metadata.EncryptionKeyId = encryptionResult.EncryptionKeyId;
        metadata.InitializationVector = encryptionResult.InitializationVector;
        metadata.AuthenticationTag = encryptionResult.AuthenticationTag;

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Re-encrypted image {ImageId} from key {OldKey} to key {NewKey}",
            metadata.ImageId,
            metadata.EncryptionKeyId,
            newKid);

        return metadata;
    }

    public async Task<EncryptionKeyStats> GetKeyStatsAsync(
        int kid,
        CancellationToken cancellationToken = default)
    {
        var key = await _context.EncryptionKeys
            .AsNoTracking()
            .Include(k => k.Images)
            .FirstOrDefaultAsync(k => k.Kid == kid, cancellationToken);

        if (key == null)
        {
            throw new InvalidOperationException($"Encryption key {kid} not found");
        }

        var imageCount = key.Images.Count;
        
        // Calculate total size by reading file sizes
        long totalSize = 0;
        foreach (var image in key.Images)
        {
            var filePath = Path.Combine("/app/data/images", image.FileName);
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                totalSize += fileInfo.Length;
            }
        }

        return new EncryptionKeyStats(
            Kid: key.Kid,
            Version: key.Version,
            Status: key.Status,
            ImageCount: imageCount,
            TotalSizeBytes: totalSize,
            CreatedAt: key.CreatedAt,
            ActivatedAt: key.ActivatedAt,
            RetiredAt: key.RetiredAt
        );
    }
}


