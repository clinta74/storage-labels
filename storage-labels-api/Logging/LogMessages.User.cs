using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 4000,
        Level = LogLevel.Error,
        Message = "User id ({UserId}) not found")]
    public static partial void UserNotFound(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "Retrieved {Count} users")]
    public static partial void UsersRetrieved(
        this ILogger logger,
        int count);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Error,
        Message = "Error retrieving all users")]
    public static partial void UsersRetrievalFailed(
        this ILogger logger,
        Exception ex);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Information,
        Message = "Updated preferences for user {UserId}")]
    public static partial void UserPreferencesUpdated(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 4004,
        Level = LogLevel.Warning,
        Message = "User {UserId} not found for role update")]
    public static partial void UserNotFoundForRoleUpdate(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 4005,
        Level = LogLevel.Error,
        Message = "Failed to remove roles from user {UserId}: {Errors}")]
    public static partial void UserRoleRemovalFailed(
        this ILogger logger,
        string userId,
        string errors);

    [LoggerMessage(
        EventId = 4006,
        Level = LogLevel.Error,
        Message = "Failed to add role {Role} to user {UserId}: {Errors}")]
    public static partial void UserRoleAddFailed(
        this ILogger logger,
        string role,
        string userId,
        string errors);

    [LoggerMessage(
        EventId = 4007,
        Level = LogLevel.Information,
        Message = "Updated user {UserId} ({Email}) to role {Role}")]
    public static partial void UserRoleUpdated(
        this ILogger logger,
        string userId,
        string email,
        string role);

    [LoggerMessage(
        EventId = 4008,
        Level = LogLevel.Error,
        Message = "Error updating role for user {UserId}")]
    public static partial void UserRoleUpdateFailed(
        this ILogger logger,
        Exception ex,
        string userId);

    [LoggerMessage(
        EventId = 4009,
        Level = LogLevel.Information,
        Message = "First user registered - assigned Admin role to {Username}")]
    public static partial void FirstUserRegisteredAsAdmin(
        this ILogger logger,
        string username);

    [LoggerMessage(
        EventId = 4010,
        Level = LogLevel.Warning,
        Message = "Password change failed for user {UserId}: {Errors}")]
    public static partial void PasswordChangeFailed(
        this ILogger logger,
        string userId,
        string errors);

    [LoggerMessage(
        EventId = 4011,
        Level = LogLevel.Information,
        Message = "Password changed successfully for user {UserId}")]
    public static partial void PasswordChanged(
        this ILogger logger,
        string userId);

    [LoggerMessage(
        EventId = 4012,
        Level = LogLevel.Error,
        Message = "Admin password reset failed for user {UserId}: {Errors}")]
    public static partial void AdminPasswordResetFailed(
        this ILogger logger,
        string userId,
        string errors);

    [LoggerMessage(
        EventId = 4013,
        Level = LogLevel.Warning,
        Message = "Password reset by admin for user {UserId}")]
    public static partial void PasswordResetByAdmin(
        this ILogger logger,
        string userId);
}
