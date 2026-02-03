using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 8000,
        Level = LogLevel.Information,
        Message = "Database migrations applied successfully")]
    public static partial void DatabaseMigrationsApplied(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "Roles initialized for Local authentication mode")]
    public static partial void RolesInitialized(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Information,
        Message = "First user to register will be assigned Admin role")]
    public static partial void FirstUserWillBeAdmin(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Warning,
        Message = "RoleInitializationService not available for Local mode initialization")]
    public static partial void RoleServiceNotAvailable(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8004,
        Level = LogLevel.Information,
        Message = "Created 'anonymous' user for NoAuth mode")]
    public static partial void AnonymousUserCreated(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8005,
        Level = LogLevel.Debug,
        Message = "Anonymous user already exists")]
    public static partial void AnonymousUserExists(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8006,
        Level = LogLevel.Error,
        Message = "Failed to initialize database")]
    public static partial void DatabaseInitializationFailed(
        this ILogger logger,
        Exception ex);

    // JWT Configuration
    [LoggerMessage(
        EventId = 8007,
        Level = LogLevel.Warning,
        Message = "⚠️  JWT Secret was not configured or is a placeholder. A random secret has been generated")]
    public static partial void JwtSecretNotConfigured(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8008,
        Level = LogLevel.Warning,
        Message = "⚠️  This secret will change on every restart, invalidating all existing tokens")]
    public static partial void JwtSecretTemporary(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8009,
        Level = LogLevel.Warning,
        Message = "⚠️  For production, set a persistent JWT secret via environment variable or appsettings.json")]
    public static partial void JwtSecretProductionWarning(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8010,
        Level = LogLevel.Information,
        Message = "Generated JWT Secret (save this for persistence): {Secret}")]
    public static partial void JwtSecretGenerated(
        this ILogger logger,
        string secret);

    // Role Initialization Service
    [LoggerMessage(
        EventId = 8011,
        Level = LogLevel.Information,
        Message = "No default admin credentials configured - skipping admin creation")]
    public static partial void NoDefaultAdminConfigured(
        this ILogger logger);

    [LoggerMessage(
        EventId = 8012,
        Level = LogLevel.Information,
        Message = "Default admin user '{Username}' already exists")]
    public static partial void DefaultAdminExists(
        this ILogger logger,
        string username);

    [LoggerMessage(
        EventId = 8013,
        Level = LogLevel.Information,
        Message = "Default admin user '{Username}' created successfully")]
    public static partial void DefaultAdminCreated(
        this ILogger logger,
        string username);

    [LoggerMessage(
        EventId = 8014,
        Level = LogLevel.Error,
        Message = "Failed to create default admin user: {Errors}")]
    public static partial void DefaultAdminCreationFailed(
        this ILogger logger,
        string errors);

    [LoggerMessage(
        EventId = 8015,
        Level = LogLevel.Information,
        Message = "Role '{RoleName}' created")]
    public static partial void RoleCreated(
        this ILogger logger,
        string roleName);

    [LoggerMessage(
        EventId = 8016,
        Level = LogLevel.Error,
        Message = "Failed to create role '{RoleName}': {Errors}")]
    public static partial void RoleCreationFailed(
        this ILogger logger,
        string roleName,
        string errors);

    [LoggerMessage(
        EventId = 8017,
        Level = LogLevel.Information,
        Message = "Request was cancelled")]
    public static partial void RequestCancelled(
        this ILogger logger);
}
