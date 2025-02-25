internal static partial class LogMessages
{
    [LoggerMessage(Message = "User id ({userId}) not found.", Level = LogLevel.Error)]
    internal static partial void UserNotFound(this ILogger logger, string UserId);

    [LoggerMessage(Message = "User id ({userId}) cannot add box to location id ({locationId}).", Level = LogLevel.Warning)]
    internal static partial void NoAccessToLocation(this ILogger logger, string userId, long locationId);

#region Box Logs
    [LoggerMessage(Message = "Box with id ({boxId}) deleted.", Level = LogLevel.Information)]
    internal static partial void DeleteBox(this ILogger logger, Guid boxId);
#endregion
#region Location Logs
    [LoggerMessage(Message = "Location with id ({locationId}) deleted.", Level = LogLevel.Information)]
    internal static partial void DeleteLocation(this ILogger logger, long locationId);
#endregion
}