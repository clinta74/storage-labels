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
    Local,
    
    /// <summary>
    /// Auth0 authentication (legacy mode for backward compatibility)
    /// </summary>
    Auth0,
    
    /// <summary>
    /// Generic OpenID Connect provider (Authentik, Keycloak, etc.)
    /// </summary>
    OIDC
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
    public LocalAuthSettings? Local { get; set; }

    /// <summary>
    /// Auth0 settings (legacy)
    /// </summary>
    public Auth0Settings? Auth0 { get; set; }

    /// <summary>
    /// OpenID Connect settings
    /// </summary>
    public OIDCSettings? OIDC { get; set; }

    /// <summary>
    /// External provider settings
    /// </summary>
    public ExternalProvidersSettings? ExternalProviders { get; set; }
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

/// <summary>
/// Generic OIDC provider settings
/// </summary>
public class OIDCSettings
{
    /// <summary>
    /// Whether OIDC is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// OIDC authority (issuer URL)
    /// </summary>
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// OIDC client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OIDC client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Response type (default: code)
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Scopes to request
    /// </summary>
    public string[] Scopes { get; set; } = ["openid", "profile", "email"];
}

/// <summary>
/// External authentication providers settings
/// </summary>
public class ExternalProvidersSettings
{
    /// <summary>
    /// Google OAuth settings
    /// </summary>
    public OAuthProviderSettings? Google { get; set; }

    /// <summary>
    /// Microsoft OAuth settings
    /// </summary>
    public OAuthProviderSettings? Microsoft { get; set; }

    /// <summary>
    /// GitHub OAuth settings
    /// </summary>
    public OAuthProviderSettings? GitHub { get; set; }
}

/// <summary>
/// OAuth provider settings
/// </summary>
public class OAuthProviderSettings
{
    /// <summary>
    /// Whether this provider is enabled
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// OAuth client ID
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// OAuth client secret
    /// </summary>
    public string ClientSecret { get; set; } = string.Empty;
}
