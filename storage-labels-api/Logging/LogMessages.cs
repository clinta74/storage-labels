internal static partial class LogMessages
{
    [LoggerMessage(Message = "User id ({userId}) not found.", Level = LogLevel.Error)]
    internal static partial void LogUserNotFound(this ILogger logger, string UserId);

    [LoggerMessage(Message = "Box with id ({boxId})", Level = LogLevel.Information)]
    internal static partial void LogDeleteBox(this ILogger logger, Guid boxId);

    [LoggerMessage(Message = "User id ({userId}) cannot add box to location id ({locationId}).", Level = LogLevel.Warning)]
    internal static partial void LogNoAccessToLocation(this ILogger logger, string userId, long locationId);
}