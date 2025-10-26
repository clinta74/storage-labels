using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(Message = "Location with id ({locationId}) deleted.", Level = LogLevel.Information)]
    public static partial void DeleteLocation(this ILogger logger, long locationId);
}
