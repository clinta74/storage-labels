namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        Message = "Database migrations applied successfully",
        Level = LogLevel.Information)]
    public static partial void DatabaseMigrationsApplied(this ILogger logger);

    [LoggerMessage(
        Message = "Roles initialized for Local authentication mode",
        Level = LogLevel.Information)]
    public static partial void RolesInitialized(this ILogger logger);

    [LoggerMessage(
        Message = "First user to register will be assigned Admin role",
        Level = LogLevel.Information)]
    public static partial void FirstUserWillBeAdmin(this ILogger logger);

    [LoggerMessage(
        Message = "RoleInitializationService not available for Local mode initialization",
        Level = LogLevel.Warning)]
    public static partial void RoleServiceNotAvailable(this ILogger logger);

    [LoggerMessage(
        Message = "Created 'anonymous' user for NoAuth mode",
        Level = LogLevel.Information)]
    public static partial void AnonymousUserCreated(this ILogger logger);

    [LoggerMessage(
        Message = "Anonymous user already exists",
        Level = LogLevel.Debug)]
    public static partial void AnonymousUserExists(this ILogger logger);

    [LoggerMessage(
        Message = "Failed to initialize database",
        Level = LogLevel.Error)]
    public static partial void DatabaseInitializationFailed(this ILogger logger, Exception ex);
}
