using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 2000,
        Level = LogLevel.Warning,
        Message = "User ({UserId}) attempted to add an item to box ({BoxId})")]
    public static partial void LogItemAddAttemptWarning(
        this ILogger logger,
        string userId,
        Guid boxId);
}
