using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 7000,
        Level = LogLevel.Information,
        Message = "Location with id ({LocationId}) deleted")]
    public static partial void DeleteLocation(
        this ILogger logger,
        long locationId);
}
