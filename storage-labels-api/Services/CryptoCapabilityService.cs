using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;

namespace StorageLabelsApi.Services;

/// <summary>
/// Service for detecting cryptographic hardware capabilities
/// </summary>
public class CryptoCapabilityService
{
    /// <summary>
    /// Detects available cryptographic hardware acceleration capabilities
    /// </summary>
    public CryptoCapabilities DetectCapabilities()
    {
        return new CryptoCapabilities
        {
            SupportsAesGcm = IsAesGcmSupported(),
            HasAesNi = IsAesNiSupported(),
            HasAvx2 = IsAvx2Supported(),
            HasSse2 = IsSse2Supported()
        };
    }

    private static bool IsAesGcmSupported()
    {
        try
        {
            // Test if AesGcm is available
            using var test = new AesGcm(new byte[32], 16);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsAesNiSupported()
    {
        // Check for AES-NI hardware acceleration (x86/x64 only)
        return System.Runtime.Intrinsics.X86.Aes.IsSupported;
    }

    private static bool IsAvx2Supported()
    {
        // Check for AVX2 support (x86/x64 only)
        return Avx2.IsSupported;
    }

    private static bool IsSse2Supported()
    {
        // Check for SSE2 support (x86/x64 only)
        return Sse2.IsSupported;
    }
}

/// <summary>
/// Information about available cryptographic capabilities
/// </summary>
public class CryptoCapabilities
{
    /// <summary>
    /// Whether AES-GCM authenticated encryption is supported
    /// </summary>
    public bool SupportsAesGcm { get; init; }

    /// <summary>
    /// Whether AES-NI hardware acceleration is available
    /// </summary>
    public bool HasAesNi { get; init; }

    /// <summary>
    /// Whether AVX2 SIMD instructions are available
    /// </summary>
    public bool HasAvx2 { get; init; }

    /// <summary>
    /// Whether SSE2 SIMD instructions are available
    /// </summary>
    public bool HasSse2 { get; init; }

    /// <summary>
    /// Gets a human-readable summary of capabilities
    /// </summary>
    public string GetSummary()
    {
        var features = new List<string>();
        
        if (HasAesNi) features.Add("AES-NI");
        if (HasAvx2) features.Add("AVX2");
        if (HasSse2) features.Add("SSE2");

        var hardware = features.Count > 0 
            ? string.Join(", ", features)
            : "Software-only";

        return $"AES-GCM: {(SupportsAesGcm ? "Supported" : "Not supported")}, Hardware: {hardware}";
    }
}
