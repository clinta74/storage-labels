using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(Message = "Box with id ({boxId}) deleted.", Level = LogLevel.Information)]
    public static partial void DeleteBox(this ILogger logger, Guid boxId);

    [LoggerMessage(Message = "User id ({userId}) cannot add box to location id ({locationId}).", Level = LogLevel.Warning)]
    public static partial void NoAccessToLocation(this ILogger logger, string userId, long locationId);
}
