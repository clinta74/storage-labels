namespace StorageLabelsApi.Models.Settings;

/// <summary>
/// Configuration for refresh token issuance and cookies.
/// </summary>
public class RefreshTokenSettings
{
    /// <summary>
    /// Cookie name for storing the refresh token.
    /// </summary>
    public string CookieName { get; set; } = "sl_refresh_token";

    /// <summary>
    /// Lifetime in minutes for short-lived refresh tokens.
    /// </summary>
    public int LifetimeMinutes { get; set; } = 60 * 24; // 1 day

    /// <summary>
    /// Lifetime in days for persistent (remember me) refresh tokens.
    /// </summary>
    public int PersistentLifetimeDays { get; set; } = 30;

    /// <summary>
    /// Validate settings are sane.
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(CookieName))
        {
            throw new ArgumentException("Refresh token cookie name is required");
        }

        if (LifetimeMinutes <= 0)
        {
            throw new ArgumentException("Refresh token lifetime must be positive");
        }

        if (PersistentLifetimeDays <= 0)
        {
            throw new ArgumentException("Persistent refresh token lifetime must be positive");
        }
    }

    /// <summary>
    /// Resolve the lifetime to use based on persistence.
    /// </summary>
    public TimeSpan GetLifetime(bool persistent)
    {
        return persistent
            ? TimeSpan.FromDays(PersistentLifetimeDays)
            : TimeSpan.FromMinutes(LifetimeMinutes);
    }
}
