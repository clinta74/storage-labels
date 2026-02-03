using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 3000,
        Level = LogLevel.Information,
        Message = "User ({Username}) login attempt")]
    public static partial void LoginAttempt(
        this ILogger logger,
        string username);
    
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Warning,
        Message = "User ({Username}) login failed - invalid credentials")]
    public static partial void LoginFailed(
        this ILogger logger,
        string username);
    
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "User ({Username}) login succeeded")]
    public static partial void LoginSucceeded(
        this ILogger logger,
        string username);
    
    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Information,
        Message = "User ({Username}) registration attempt")]
    public static partial void RegistrationAttempt(
        this ILogger logger,
        string username);
    
    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Information,
        Message = "User ({Username}) registration succeeded")]
    public static partial void RegistrationSucceeded(
        this ILogger logger,
        string username);
    
    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Warning,
        Message = "User ({Username}) registration failed - {Reason}")]
    public static partial void RegistrationFailed(
        this ILogger logger,
        string username,
        string reason);
    
    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Warning,
        Message = "User ({UserId}) account is locked")]
    public static partial void AccountLocked(
        this ILogger logger,
        string userId);
    
    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Warning,
        Message = "User ({UserId}) account is inactive")]
    public static partial void AccountInactive(
        this ILogger logger,
        string userId);
    
    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Information,
        Message = "User ({UserId}) logged out")]
    public static partial void UserLoggedOut(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 3009,
        Level = LogLevel.Information,
        Message = "User ({UserId}) refresh attempt")]
    public static partial void RefreshAttempt(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 3010,
        Level = LogLevel.Information,
        Message = "User ({UserId}) refresh succeeded")]
    public static partial void RefreshSucceeded(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 3011,
        Level = LogLevel.Warning,
        Message = "Refresh token failed for user ({UserId}) - {Reason}")]
    public static partial void RefreshFailed(
        this ILogger logger,
        string userId,
        string reason);

    [LoggerMessage(
        EventId = 3012,
        Level = LogLevel.Information,
        Message = "Revoked {Count} refresh tokens for user ({UserId})")]
    public static partial void RefreshTokensRevoked(
        this ILogger logger,
        string userId,
        int count);

    [LoggerMessage(
        EventId = 3013,
        Level = LogLevel.Debug,
        Message = "Issued refresh token ({TokenId}) for user ({UserId})")]
    public static partial void RefreshTokenIssued(
        this ILogger logger,
        string userId,
        Guid tokenId);

    [LoggerMessage(
        EventId = 3014,
        Level = LogLevel.Warning,
        Message = "Refresh token not found")]
    public static partial void RefreshTokenMissing(
        this ILogger logger);

    [LoggerMessage(
        EventId = 3015,
        Level = LogLevel.Warning,
        Message = "Refresh token for user ({UserId}) already revoked")]
    public static partial void RefreshTokenAlreadyRevoked(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 3016,
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user ({UserId})")]
    public static partial void RefreshTokenReuseDetected(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 3017,
        Level = LogLevel.Information,
        Message = "Refresh token expired for user ({UserId})")]
    public static partial void RefreshTokenExpired(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 3018,
        Level = LogLevel.Information,
        Message = "Rotated refresh token for user ({UserId}) to token ({TokenId})")]
    public static partial void RefreshTokenRotated(
        this ILogger logger,
        string userId,
        Guid tokenId);
}
