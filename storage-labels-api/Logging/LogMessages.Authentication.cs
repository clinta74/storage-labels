using System;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(Message = "User ({username}) login attempt.", Level = LogLevel.Information)]
    public static partial void LoginAttempt(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) login failed - invalid credentials.", Level = LogLevel.Warning)]
    public static partial void LoginFailed(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) login succeeded.", Level = LogLevel.Information)]
    public static partial void LoginSucceeded(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) registration attempt.", Level = LogLevel.Information)]
    public static partial void RegistrationAttempt(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) registration succeeded.", Level = LogLevel.Information)]
    public static partial void RegistrationSucceeded(this ILogger logger, string username);
    
    [LoggerMessage(Message = "User ({username}) registration failed - {reason}.", Level = LogLevel.Warning)]
    public static partial void RegistrationFailed(this ILogger logger, string username, string reason);
    
    [LoggerMessage(Message = "User ({userId}) account is locked.", Level = LogLevel.Warning)]
    public static partial void AccountLocked(this ILogger logger, string userId);
    
    [LoggerMessage(Message = "User ({userId}) account is inactive.", Level = LogLevel.Warning)]
    public static partial void AccountInactive(this ILogger logger, string userId);
    
    [LoggerMessage(Message = "User ({userId}) logged out.", Level = LogLevel.Information)]
    public static partial void UserLoggedOut(this ILogger logger, string userId);

    [LoggerMessage(Message = "User ({userId}) refresh attempt.", Level = LogLevel.Information)]
    public static partial void RefreshAttempt(this ILogger logger, string userId);

    [LoggerMessage(Message = "User ({userId}) refresh succeeded.", Level = LogLevel.Information)]
    public static partial void RefreshSucceeded(this ILogger logger, string userId);

    [LoggerMessage(Message = "Refresh token failed for user ({userId}) - {reason}.", Level = LogLevel.Warning)]
    public static partial void RefreshFailed(this ILogger logger, string userId, string reason);

    [LoggerMessage(Message = "Revoked {count} refresh tokens for user ({userId}).", Level = LogLevel.Information)]
    public static partial void RefreshTokensRevoked(this ILogger logger, string userId, int count);

    [LoggerMessage(Message = "Issued refresh token ({tokenId}) for user ({userId}).", Level = LogLevel.Debug)]
    public static partial void RefreshTokenIssued(this ILogger logger, string userId, Guid tokenId);

    [LoggerMessage(Message = "Refresh token not found.", Level = LogLevel.Warning)]
    public static partial void RefreshTokenMissing(this ILogger logger);

    [LoggerMessage(Message = "Refresh token for user ({userId}) already revoked.", Level = LogLevel.Warning)]
    public static partial void RefreshTokenAlreadyRevoked(this ILogger logger, string userId);

    [LoggerMessage(Message = "Refresh token reuse detected for user ({userId}).", Level = LogLevel.Warning)]
    public static partial void RefreshTokenReuseDetected(this ILogger logger, string userId);

    [LoggerMessage(Message = "Refresh token expired for user ({userId}).", Level = LogLevel.Information)]
    public static partial void RefreshTokenExpired(this ILogger logger, string userId);

    [LoggerMessage(Message = "Rotated refresh token for user ({userId}) to token ({tokenId}).", Level = LogLevel.Information)]
    public static partial void RefreshTokenRotated(this ILogger logger, string userId, Guid tokenId);
}
