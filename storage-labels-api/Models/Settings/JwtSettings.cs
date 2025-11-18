namespace StorageLabelsApi.Models.Settings;

/// <summary>
/// JWT token settings
/// </summary>
public class JwtSettings
{
    /// <summary>
    /// Secret key for signing JWT tokens (min 32 characters for HS256)
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer
    /// </summary>
    public string Issuer { get; set; } = "storage-labels-api";

    /// <summary>
    /// Token audience
    /// </summary>
    public string Audience { get; set; } = "storage-labels-ui";

    /// <summary>
    /// Token expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Check if the secret is a placeholder value
    /// </summary>
    public bool IsSecretPlaceholder()
    {
        return string.IsNullOrWhiteSpace(Secret) ||
               Secret == "your-secret-key-min-32-chars-change-in-production" ||
               Secret.Contains("change-in-production", StringComparison.OrdinalIgnoreCase) ||
               Secret.Contains("your-secret", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generate a cryptographically secure random secret
    /// </summary>
    public static string GenerateSecret()
    {
        // Generate 64 bytes (512 bits) of random data
        var randomBytes = new byte[64];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        // Convert to base64 string (88 characters)
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Validate settings
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Secret))
            throw new ArgumentException("JWT Secret is required");
        
        if (Secret.Length < 32)
            throw new ArgumentException("JWT Secret must be at least 32 characters");
        
        if (string.IsNullOrWhiteSpace(Issuer))
            throw new ArgumentException("JWT Issuer is required");
        
        if (string.IsNullOrWhiteSpace(Audience))
            throw new ArgumentException("JWT Audience is required");
    }
}
