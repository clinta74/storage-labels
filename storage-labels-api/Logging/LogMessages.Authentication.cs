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
}
