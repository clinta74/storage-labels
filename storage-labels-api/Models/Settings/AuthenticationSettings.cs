namespace StorageLabelsApi.Models.Settings;

/// <summary>
/// Authentication mode for the application
/// </summary>
public enum AuthenticationMode
{
    /// <summary>
    /// No authentication - open access (use with caution!)
    /// </summary>
    None,
    
    /// <summary>
    /// Built-in local authentication with ASP.NET Identity
    /// </summary>
    Local
}

/// <summary>
/// Main authentication configuration
/// </summary>
public class AuthenticationSettings
{
    /// <summary>
    /// Current authentication mode
    /// </summary>
    public AuthenticationMode Mode { get; set; } = AuthenticationMode.Local;

    /// <summary>
    /// Local authentication settings
    /// </summary>
    public LocalAuthSettings Local { get; set; } = new();
}

/// <summary>
/// Local authentication settings
/// </summary>
public class LocalAuthSettings
{
    /// <summary>
    /// Whether local auth is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Allow new user registration
    /// </summary>
    public bool AllowRegistration { get; set; } = true;

    /// <summary>
    /// Require email confirmation
    /// </summary>
    public bool RequireEmailConfirmation { get; set; } = false;

    /// <summary>
    /// Minimum password length
    /// </summary>
    public int MinimumPasswordLength { get; set; } = 8;

    /// <summary>
    /// Require digit in password
    /// </summary>
    public bool RequireDigit { get; set; } = true;

    /// <summary>
    /// Require lowercase in password
    /// </summary>
    public bool RequireLowercase { get; set; } = true;

    /// <summary>
    /// Require uppercase in password
    /// </summary>
    public bool RequireUppercase { get; set; } = true;

    /// <summary>
    /// Require non-alphanumeric in password
    /// </summary>
    public bool RequireNonAlphanumeric { get; set; } = false;

    /// <summary>
    /// Lockout enabled
    /// </summary>
    public bool LockoutEnabled { get; set; } = true;

    /// <summary>
    /// Max failed access attempts before lockout
    /// </summary>
    public int MaxFailedAccessAttempts { get; set; } = 5;

    /// <summary>
    /// Lockout duration in minutes
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;
}
