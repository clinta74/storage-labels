using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(Message = "User id ({userId}) not found.", Level = LogLevel.Error)]
    public static partial void UserNotFound(this ILogger logger, string UserId);
}
