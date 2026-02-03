using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 11000,
        Level = LogLevel.Error,
        Message = "Error checking authorization requirement")]
    public static partial void AuthorizationCheckFailed(
        this ILogger logger,
        Exception ex);
}
