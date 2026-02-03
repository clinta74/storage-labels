using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 6000,
        Level = LogLevel.Information,
        Message = "Box with id ({BoxId}) deleted")]
    public static partial void DeleteBox(
        this ILogger logger,
        Guid boxId);

    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Warning,
        Message = "User id ({UserId}) cannot add box to location id ({LocationId})")]
    public static partial void NoAccessToLocation(
        this ILogger logger,
        string userId,
        long locationId);
}
