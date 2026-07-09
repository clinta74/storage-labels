using Microsoft.Extensions.Logging;

namespace StorageLabelsApi.Logging;

public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 11000,
        Level = LogLevel.Information,
        Message = "User ({UserId}) created label job ({JobId})")]
    public static partial void LabelJobCreated(
        this ILogger logger,
        string userId,
        Guid jobId);

    [LoggerMessage(
        EventId = 11001,
        Level = LogLevel.Information,
        Message = "User ({UserId}) generated label page for job ({JobId}), count ({Count})")]
    public static partial void LabelPageGenerated(
        this ILogger logger,
        string userId,
        Guid jobId,
        int count);

    [LoggerMessage(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "User ({UserId}) deleted label job ({JobId})")]
    public static partial void LabelJobDeleted(
        this ILogger logger,
        string userId,
        Guid jobId);

    [LoggerMessage(
        EventId = 11003,
        Level = LogLevel.Information,
        Message = "User ({UserId}) updated label job ({JobId})")]
    public static partial void LabelJobUpdated(
        this ILogger logger,
        string userId,
        Guid jobId);
}
