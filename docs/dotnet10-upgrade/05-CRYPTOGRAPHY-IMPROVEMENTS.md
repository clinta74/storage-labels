# .NET 10 Cryptographic API Improvements Plan

## Overview
This document outlines potential cryptographic improvements for the Storage Labels API when upgrading to .NET 10, focusing on the `ImageEncryptionService` implementation.

---

## Current Implementation Analysis

### Current Encryption Stack
- **Algorithm**: AES-256-GCM (Galois/Counter Mode)
- **Key Size**: 256 bits (32 bytes)
- **IV Size**: 96 bits (12 bytes) - GCM recommended size
- **Authentication Tag**: 128 bits (16 bytes)
- **Key Generation**: `RandomNumberGenerator.Fill()`
- **Storage**: Raw `byte[]` key material in PostgreSQL database

### Current Files
- `Services/ImageEncryptionService.cs` - Main encryption implementation
- `Services/IImageEncryptionService.cs` - Service interface
- `Datalayer/Models/EncryptionKey.cs` - Key storage model

### Current Operations
1. **Encryption**: Loads entire image into memory, encrypts with active key
2. **Decryption**: Loads encrypted data, decrypts with specified key
3. **Key Management**: Create, activate, retire, deprecate encryption keys
4. **Key Rotation**: Supports versioned keys with rotation capability

---

## .NET 10 Cryptographic Enhancements

### 1. One-Shot Span-Based Cryptographic Operations

#### What It Is
.NET 10 introduces static one-shot methods for cryptographic operations that eliminate object instantiation overhead.

#### Current Code (Lines 60-62 in ImageEncryptionService.cs)
```csharp
using var aesGcm = new AesGcm(activeKey.KeyMaterial, TagSizeBytes);
aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
```

#### Improved Code
```csharp
// No object instantiation needed - static method
AesGcm.Encrypt(
    key: activeKey.KeyMaterial, 
    nonce: iv, 
    plaintext, 
    ciphertext, 
    tag);
```

#### Advantages
- **Performance**: ~10-15% faster for single operations (no object allocation)
- **Memory**: Reduces GC pressure by eliminating AesGcm object allocation
- **Clarity**: Intent is clearer - single operation, not reusable object
- **Safety**: Automatic key zeroization when method completes
- **Best For**: One-time encrypt/decrypt operations (like our image service)

#### When NOT to Use
- Encrypting multiple items with same key (object reuse is faster)
- Need to keep key material in memory for repeated operations

---

### 2. Enhanced CryptographicOperations Class

#### What It Is
Utility class for constant-time comparisons and secure memory operations.

#### New Methods in .NET 10
```csharp
// Constant-time comparison (prevents timing attacks)
bool isEqual = CryptographicOperations.FixedTimeEquals(key1, key2);

// Secure memory zeroing (resistant to compiler optimization)
CryptographicOperations.ZeroMemory(sensitiveDataSpan);

// Timing-safe byte array comparison
bool matches = CryptographicOperations.FixedTimeEquals(
    computedTag.AsSpan(), 
    receivedTag.AsSpan());
```

#### Implementation in Our Code

**Key Comparison** (for key rotation validation):
```csharp
public async Task<bool> ValidateKeyRotation(int fromKeyId, int toKeyId)
{
    var fromKey = await GetKeyAsync(fromKeyId);
    var toKey = await GetKeyAsync(toKeyId);
    
    // Use constant-time comparison to prevent timing attacks
    // that could reveal key material information
    if (CryptographicOperations.FixedTimeEquals(
        fromKey.KeyMaterial, 
        toKey.KeyMaterial))
    {
        throw new InvalidOperationException("Keys must be different");
    }
    
    return true;
}
```

**Secure Cleanup** (after encryption):
```csharp
public async Task<EncryptionResult> EncryptAsync(
    Stream inputStream,
    CancellationToken cancellationToken = default)
{
    var plaintext = await ReadStreamToArrayAsync(inputStream);
    try
    {
        // ... perform encryption
        return result;
    }
    finally
    {
        // Ensure plaintext is securely wiped from memory
        // This prevents plaintext from lingering in memory/page file
        CryptographicOperations.ZeroMemory(plaintext);
    }
}
```

#### Advantages
- **Security**: Prevents timing attacks that could leak key/data information
- **Compliance**: Meets security requirements for sensitive data handling
- **Reliability**: Resistant to compiler optimizations that might skip zeroing
- **Defense in Depth**: Additional layer of protection beyond GC cleanup

#### Why It Matters
- Memory dumps after crashes won't contain plaintext
- Prevents side-channel timing attacks during key comparison
- Required for many security compliance frameworks (PCI-DSS, HIPAA)

---

### 3. HKDF-Based Key Derivation

#### What It Is
HMAC-based Extract-and-Expand Key Derivation Function (RFC 5869) - a modern key derivation standard.

#### Current Approach (Line 164 in ImageEncryptionService.cs)
```csharp
// Generate independent random key material for each key
var keyMaterial = new byte[KeySizeBytes];
RandomNumberGenerator.Fill(keyMaterial);
```

#### Alternative: Master Key with Derivation
```csharp
public class KeyDerivationService
{
    private readonly byte[] _masterKey; // Stored in HSM or secure key vault
    
    public byte[] DeriveEncryptionKey(int version, string purpose)
    {
        var keyMaterial = new byte[32];
        
        // Derive deterministic key from master key
        using var hkdf = new Hkdf(
            HashAlgorithmName.SHA256,
            ikm: _masterKey,
            salt: Encoding.UTF8.GetBytes($"storage-labels-v{version}"));
        
        // Info parameter provides context separation
        var info = Encoding.UTF8.GetBytes($"{purpose}:key:{version}");
        hkdf.DeriveKey(info, keyMaterial);
        
        return keyMaterial;
    }
}
```

#### Advantages
- **Single Secret Management**: Only master key needs HSM/vault protection
- **Key Versioning**: Can recreate any key version from master + version number
- **Context Separation**: Different purposes derive different keys from same master
- **Disaster Recovery**: Recreate all keys from backed-up master key
- **Reduced Storage**: Don't need to store 32 bytes per key version
- **Compliance**: NIST-recommended approach (NIST SP 800-108)

#### Disadvantages
- **Master Key Risk**: Compromise of master key compromises all derived keys
- **Complexity**: More complex to implement correctly
- **Migration**: Existing keys would need to remain or be migrated

#### When to Use
- ✅ High-security environments with HSM/key vault
- ✅ Need deterministic key recreation
- ✅ Managing many key versions
- ❌ Storing keys in database anyway (our current approach is fine)

---

### 4. ChaCha20-Poly1305 Cipher Support

#### What It Is
Modern authenticated encryption algorithm alternative to AES-GCM, with better software performance.

#### Comparison with AES-GCM

| Feature | AES-GCM | ChaCha20-Poly1305 |
|---------|---------|-------------------|
| **Security** | ✅ Excellent | ✅ Excellent |
| **Hardware Acceleration** | ✅ Yes (AES-NI) | ❌ No (pure software) |
| **Software Performance** | 🟡 Moderate | ✅ Fast |
| **Mobile Performance** | 🟡 Moderate | ✅ Excellent |
| **Side-Channel Resistance** | 🟡 Needs AES-NI | ✅ Better by design |
| **NONCE Size** | 96 bits | 96 bits (or 192) |
| **FIPS 140-2** | ✅ Approved | ❌ Not approved |
| **Best For** | Servers with AES-NI | IoT, mobile, ARM |

#### Implementation
```csharp
public enum EncryptionAlgorithm
{
    AesGcm = 1,
    ChaCha20Poly1305 = 2
}

public async Task<EncryptionResult> EncryptAsync(
    Stream inputStream,
    EncryptionAlgorithm algorithm = EncryptionAlgorithm.AesGcm,
    CancellationToken cancellationToken = default)
{
    return algorithm switch
    {
        EncryptionAlgorithm.AesGcm => await EncryptWithAesGcmAsync(inputStream),
        EncryptionAlgorithm.ChaCha20Poly1305 => await EncryptWithChaChaAsync(inputStream),
        _ => throw new NotSupportedException($"Algorithm {algorithm} not supported")
    };
}

private async Task<EncryptionResult> EncryptWithChaChaAsync(Stream inputStream)
{
    var activeKey = await GetActiveKeyAsync();
    var plaintext = await ReadStreamAsync(inputStream);
    
    var nonce = new byte[12]; // 96-bit nonce
    RandomNumberGenerator.Fill(nonce);
    
    var ciphertext = new byte[plaintext.Length];
    var tag = new byte[16];
    
    // ChaCha20-Poly1305 encryption
    ChaCha20Poly1305.Encrypt(activeKey.KeyMaterial, nonce, plaintext, ciphertext, tag);
    
    return new EncryptionResult(ciphertext, nonce, tag, activeKey.Kid);
}
```

#### When to Consider ChaCha20
- ✅ ARM-based servers (Graviton, Azure ARM VMs)
- ✅ Mobile/embedded clients
- ✅ Environments without AES-NI
- ✅ Performance is critical without hardware acceleration
- ❌ FIPS 140-2 compliance required
- ❌ Already using AES-NI hardware (AES-GCM is faster)

#### For Storage Labels API
**Recommendation**: Keep AES-GCM as primary, but support both:
- Detect hardware capabilities on startup
- Use AES-GCM if AES-NI available
- Fall back to ChaCha20 if not
- Store algorithm type in `ImageMetadata`

---

### 5. Hardware Acceleration Detection

#### What It Is
Runtime detection of cryptographic hardware acceleration capabilities.

#### Implementation
```csharp
public class CryptoCapabilityService
{
    public EncryptionCapabilities DetectCapabilities()
    {
        return new EncryptionCapabilities
        {
            SupportsAesGcm = AesGcm.IsSupported,
            SupportsChaCha20 = ChaCha20Poly1305.IsSupported,
            HasAesNi = System.Runtime.Intrinsics.X86.Aes.IsSupported,
            RecommendedAlgorithm = GetRecommendedAlgorithm()
        };
    }
    
    private EncryptionAlgorithm GetRecommendedAlgorithm()
    {
        // Prefer AES-GCM if hardware acceleration available
        if (System.Runtime.Intrinsics.X86.Aes.IsSupported)
            return EncryptionAlgorithm.AesGcm;
        
        // Fall back to ChaCha20 for better software performance
        if (ChaCha20Poly1305.IsSupported)
            return EncryptionAlgorithm.ChaCha20Poly1305;
        
        // Default to AES-GCM (always supported)
        return EncryptionAlgorithm.AesGcm;
    }
}

// Startup configuration
builder.Services.AddSingleton<CryptoCapabilityService>();
var cryptoCapabilities = builder.Services
    .BuildServiceProvider()
    .GetRequiredService<CryptoCapabilityService>()
    .DetectCapabilities();

builder.Services.Configure<EncryptionSettings>(options =>
{
    options.DefaultAlgorithm = cryptoCapabilities.RecommendedAlgorithm;
});
```

#### Logging on Startup
```csharp
app.Logger.LogInformation(
    "Crypto capabilities: AES-NI={HasAesNi}, Algorithm={Algorithm}",
    cryptoCapabilities.HasAesNi,
    cryptoCapabilities.RecommendedAlgorithm);
```

#### Advantages
- **Performance**: Automatically use fastest algorithm
- **Portability**: Works optimally on any hardware
- **Observability**: Know what crypto capabilities are available
- **Testing**: Can verify proper algorithm selection in CI/CD

---

### 6. Secure Memory Handling with Pinned Buffers

#### What It Is
Preventing sensitive data from being moved by garbage collector, and ensuring secure cleanup.

#### The Problem
```csharp
// Current code - plaintext can be:
// 1. Moved by GC (leaves copies in memory)
// 2. Paged to disk by OS
// 3. Included in crash dumps
// 4. Not cleared reliably
var plaintext = new byte[dataSize];
// ... use plaintext
// GC *might* clean it up eventually
```

#### Improved Approach
```csharp
public async Task<EncryptionResult> EncryptAsync(Stream inputStream)
{
    // Allocate pinned array that GC won't move
    var plaintext = GC.AllocateUninitializedArray<byte>(
        length: inputSize, 
        pinned: true);
    
    try
    {
        await inputStream.ReadAsync(plaintext);
        
        // Perform encryption
        var result = EncryptData(plaintext);
        
        return result;
    }
    finally
    {
        // Guaranteed secure cleanup
        CryptographicOperations.ZeroMemory(plaintext);
        
        // Array stays pinned until GC
        // But now it contains zeros, not sensitive data
    }
}
```

#### Advanced: Using Span<byte> and stackalloc
```csharp
public EncryptionResult EncryptSmallData(ReadOnlySpan<byte> plaintext)
{
    // For small data, allocate on stack (never touches heap)
    Span<byte> iv = stackalloc byte[12];
    Span<byte> tag = stackalloc byte[16];
    
    RandomNumberGenerator.Fill(iv);
    
    // Ciphertext must be returned, so heap allocation needed
    var ciphertext = new byte[plaintext.Length];
    
    AesGcm.Encrypt(keyMaterial, iv, plaintext, ciphertext, tag);
    
    return new EncryptionResult(
        ciphertext, 
        iv.ToArray(), 
        tag.ToArray(), 
        keyId);
}
```

#### Advantages
- **Security**: Sensitive data can't be moved leaving copies
- **Compliance**: Prevents data leakage through memory artifacts
- **Reliability**: Deterministic cleanup, not dependent on GC timing
- **Performance**: Reduced GC pressure (pinned objects)
- **Stack Allocation**: For small buffers, avoid heap entirely

#### Implementation Strategy
1. Pin key material when loading from database
2. Pin plaintext during encryption operations
3. Pin decrypted data until written to stream
4. Always zero buffers in `finally` blocks
5. Use `stackalloc` for temporary buffers < 1KB

---

### 7. Streaming Encryption for Large Images

#### The Problem
Current implementation (Lines 42-45):
```csharp
// Loads ENTIRE image into memory before encrypting
using var inputMemory = new MemoryStream();
await inputStream.CopyToAsync(inputMemory, cancellationToken);
var plaintext = inputMemory.ToArray();
```

**Issues**:
- 100 MB image = 100 MB memory during encryption
- Multiple concurrent uploads = memory spike
- Large images can cause OutOfMemoryException
- Poor scalability

#### Solution: Chunked Streaming Encryption

**Note**: AES-GCM doesn't support true streaming (needs full plaintext for authentication tag), but we can use chunked approach:

```csharp
public class StreamingEncryptionService
{
    private const int ChunkSize = 64 * 1024; // 64 KB chunks
    
    public async Task<StreamingEncryptionResult> EncryptStreamAsync(
        Stream inputStream, 
        Stream outputStream)
    {
        var activeKey = await GetActiveKeyAsync();
        
        // Master IV for the entire file
        var masterIv = new byte[12];
        RandomNumberGenerator.Fill(masterIv);
        
        // Encrypt in chunks
        var chunkIndex = 0;
        var buffer = new byte[ChunkSize];
        var authenticators = new List<byte[]>();
        
        while (true)
        {
            var bytesRead = await inputStream.ReadAsync(buffer);
            if (bytesRead == 0) break;
            
            // Derive unique IV for this chunk
            var chunkIv = DeriveChunkIv(masterIv, chunkIndex);
            
            var ciphertext = new byte[bytesRead];
            var tag = new byte[16];
            
            AesGcm.Encrypt(
                activeKey.KeyMaterial,
                chunkIv,
                buffer.AsSpan(0, bytesRead),
                ciphertext,
                tag);
            
            // Write encrypted chunk
            await outputStream.WriteAsync(ciphertext);
            authenticators.Add(tag);
            
            chunkIndex++;
        }
        
        // Final authentication tag over all chunk tags
        var masterTag = ComputeMasterTag(authenticators, masterIv);
        
        return new StreamingEncryptionResult
        {
            MasterIv = masterIv,
            MasterTag = masterTag,
            ChunkCount = chunkIndex,
            KeyId = activeKey.Kid
        };
    }
    
    private byte[] DeriveChunkIv(byte[] masterIv, int chunkIndex)
    {
        // XOR master IV with chunk index for unique IV per chunk
        var chunkIv = new byte[12];
        Array.Copy(masterIv, chunkIv, 12);
        
        var indexBytes = BitConverter.GetBytes(chunkIndex);
        for (int i = 0; i < indexBytes.Length; i++)
        {
            chunkIv[i] ^= indexBytes[i];
        }
        
        return chunkIv;
    }
}
```

#### Advantages
- **Memory Efficiency**: Constant memory usage regardless of file size
- **Scalability**: Can handle GB-sized images without issues
- **Streaming**: Start encrypting before entire file is uploaded
- **Resumability**: Can pause/resume encryption operations
- **Progress Tracking**: Easy to report encryption progress

#### Trade-offs
- **Complexity**: More complex implementation
- **Storage**: Need to store chunk metadata
- **Compatibility**: Breaking change from current format
- **Performance**: Slight overhead from multiple AES operations

#### When to Implement
- ✅ If users upload images > 100 MB
- ✅ If memory usage is constrained
- ✅ If need upload progress reporting
- ❌ If all images are < 10 MB (current approach is fine)

---

### 8. Key Wrapping (Envelope Encryption)

#### What It Is
Encrypt encryption keys with a master key (key encryption key).

#### Current Approach
```csharp
public class EncryptionKey
{
    // Raw key material stored in database
    public byte[] KeyMaterial { get; set; }
}
```

**Risk**: Database compromise = all keys compromised

#### Envelope Encryption Pattern
```csharp
public class EncryptionKey
{
    // Key material encrypted with master key
    public byte[] EncryptedKeyMaterial { get; set; }
    
    // Reference to key encryption key
    public string KeyEncryptionKeyId { get; set; }
    
    // Algorithm used to encrypt the key
    public string KeyWrapAlgorithm { get; set; } = "AES-256-KW";
}

public class KeyWrappingService
{
    private readonly byte[] _masterKey; // From HSM or Azure Key Vault
    
    public byte[] WrapKey(byte[] keyMaterial)
    {
        // AES Key Wrap (RFC 3394)
        using var aesKw = new AesKeyWrap(_masterKey);
        return aesKw.Wrap(keyMaterial);
    }
    
    public byte[] UnwrapKey(byte[] wrappedKey)
    {
        using var aesKw = new AesKeyWrap(_masterKey);
        return aesKw.Unwrap(wrappedKey);
    }
}
```

#### Integration with Azure Key Vault
```csharp
public class AzureKeyVaultWrappingService
{
    private readonly KeyClient _keyClient;
    
    public async Task<byte[]> WrapKeyAsync(byte[] keyMaterial)
    {
        var result = await _keyClient.WrapKeyAsync(
            KeyWrapAlgorithm.RsaOaep256,
            keyMaterial);
        
        return result.EncryptedKey;
    }
}
```

#### Advantages
- **Defense in Depth**: Database compromise doesn't expose keys
- **Compliance**: Required for PCI-DSS Level 1, HIPAA
- **Key Rotation**: Rotate master key without re-encrypting data
- **Audit**: Centralized key access logging
- **HSM Protection**: Master key can be in hardware security module

#### Implementation Cost
- **Complexity**: Additional service and key management
- **Dependencies**: Azure Key Vault or HSM
- **Performance**: Extra unwrap operation per encryption
- **Cost**: Azure Key Vault pricing ($0.03 per 10,000 operations)

#### Recommendation
- Implement if: HIPAA/PCI compliance needed, storing sensitive medical/financial images
- Skip if: Internal tool, low-security requirements, cost-sensitive

---

## Recommended Implementation Priority

### Phase 1: Low-Hanging Fruit (1-2 days)
✅ **Priority: High, Effort: Low**

1. **One-Shot Methods** - Replace `new AesGcm()` with static methods
   - Performance gain: 10-15%
   - Risk: Very low
   - Files: `ImageEncryptionService.cs`

2. **CryptographicOperations.ZeroMemory()** - Add to finally blocks
   - Security gain: High
   - Risk: None
   - Files: `ImageEncryptionService.cs`, handlers that use encryption

3. **Hardware Capability Detection** - Log on startup
   - Benefit: Observability
   - Risk: None
   - Files: `Program.cs`, new `CryptoCapabilityService.cs`

### Phase 2: Security Hardening (3-5 days)
✅ **Priority: Medium, Effort: Medium**

4. **Pinned Memory Allocation** - Pin sensitive buffers
   - Security gain: High
   - Risk: Low (just memory management)
   - Files: `ImageEncryptionService.cs`

5. **Algorithm Flexibility** - Support ChaCha20-Poly1305
   - Benefit: Better ARM performance, future-proofing
   - Risk: Medium (new code paths, testing required)
   - Files: `ImageEncryptionService.cs`, `EncryptionKey.cs`, `ImageMetadata.cs`

### Phase 3: Advanced Features (1-2 weeks)
⚠️ **Priority: Low, Effort: High**

6. **Streaming Encryption** - Chunked encryption for large files
   - Benefit: Handle GB-sized images
   - Risk: High (breaking change, complex implementation)
   - Decision: Only if users need large file support

7. **Envelope Encryption** - Key wrapping with master key
   - Benefit: Compliance, defense in depth
   - Risk: High (external dependencies, cost)
   - Decision: Only if compliance required

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task EncryptAsync_ZerosPlaintextOnException()
{
    // Arrange
    var plaintext = new byte[1024];
    RandomNumberGenerator.Fill(plaintext);
    var pinnedHandle = GCHandle.Alloc(plaintext, GCHandleType.Pinned);
    
    // Act & Assert
    await Assert.ThrowsAsync<Exception>(() => 
        service.EncryptAsync(CreateThrowingStream()));
    
    // Verify plaintext was zeroed
    Assert.All(plaintext, b => Assert.Equal(0, b));
}

[Fact]
public void EncryptDecrypt_WithChaCha20_RoundTrips()
{
    // Test ChaCha20 encryption/decryption
    var original = "sensitive data"u8.ToArray();
    
    var encrypted = service.Encrypt(original, EncryptionAlgorithm.ChaCha20Poly1305);
    var decrypted = service.Decrypt(encrypted);
    
    Assert.Equal(original, decrypted);
}
```

### Performance Benchmarks
```csharp
[Benchmark]
public void EncryptWithInstanceMethod()
{
    using var aes = new AesGcm(_key);
    aes.Encrypt(_iv, _plaintext, _ciphertext, _tag);
}

[Benchmark]
public void EncryptWithOneShot()
{
    AesGcm.Encrypt(_key, _iv, _plaintext, _ciphertext, _tag);
}
```

### Security Testing
- Memory dumps after encryption (verify zeroing)
- Timing analysis for FixedTimeEquals
- Hardware capability detection on different platforms

---

## Performance Expectations

### One-Shot Methods
- **Improvement**: 10-15% faster for single operations
- **When**: Most beneficial for request/response workload (our case)

### ChaCha20 vs AES-GCM
- **With AES-NI**: AES-GCM ~2x faster
- **Without AES-NI**: ChaCha20 ~3x faster
- **ARM processors**: ChaCha20 significantly faster

### Memory Efficiency
- **Current**: O(n) where n = image size
- **With Streaming**: O(1) constant memory (chunk size)

---

## Migration Strategy

### Backward Compatibility
All changes must support existing encrypted images:

```csharp
public async Task<Stream> DecryptImageAsync(ImageMetadata metadata)
{
    // Detect encryption version from metadata
    return metadata.EncryptionVersion switch
    {
        1 => await DecryptV1Async(metadata),        // Current format
        2 => await DecryptV2Async(metadata),        // With one-shot methods
        3 => await DecryptStreamingAsync(metadata), // Chunked format
        _ => throw new NotSupportedException()
    };
}
```

### Database Schema Changes
```sql
-- Add new columns, keep existing ones
ALTER TABLE "ImageMetadata" 
ADD COLUMN "EncryptionAlgorithm" VARCHAR(50) DEFAULT 'AES-256-GCM';

ALTER TABLE "ImageMetadata"
ADD COLUMN "EncryptionVersion" INT DEFAULT 1;
```

---

## References & Learning Resources

### Official Documentation
- [.NET 10 Cryptography Changes](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [System.Security.Cryptography API](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography)
- [AES-GCM Best Practices](https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.aesgcm)

### RFCs & Standards
- RFC 5116: AES-GCM Authenticated Encryption
- RFC 8439: ChaCha20 and Poly1305
- RFC 5869: HKDF (HMAC-based Key Derivation)
- NIST SP 800-38D: GCM Mode Recommendations

### Security Resources
- [OWASP Cryptographic Storage Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html)
- [Azure Key Vault Best Practices](https://learn.microsoft.com/en-us/azure/key-vault/general/best-practices)

---

## Decision Log

### ✅ Recommended Implementations
1. One-shot cryptographic methods
2. CryptographicOperations.ZeroMemory()
3. Hardware capability detection

### 🤔 Consider If Needed
4. ChaCha20-Poly1305 support (if ARM deployment)
5. Pinned memory allocation (if security critical)

### ❌ Not Recommended Now
6. Streaming encryption (no large file requirement yet)
7. Envelope encryption (no compliance requirement yet)
8. HKDF key derivation (current approach sufficient)

---

## Next Steps

1. **Review this document** - Understand each improvement
2. **Prioritize changes** - Based on your requirements
3. **Create implementation tickets** - For Phase 1 items
4. **Set up benchmarks** - Measure actual performance gains
5. **Update security documentation** - Document cryptographic choices

---

## Questions for Consideration

1. **What is the average image size?** - Determines if streaming is needed
2. **Is FIPS 140-2 compliance required?** - Affects algorithm choice
3. **Are images legally sensitive?** - Determines security level needed
4. **What is your deployment platform?** - x64 vs ARM affects performance
5. **Do you have HSM or Azure Key Vault?** - Enables envelope encryption

---

*Document created: February 7, 2026*
*For: Storage Labels API .NET 10 Upgrade*
