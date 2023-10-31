using Microsoft.Extensions.Logging;

namespace Common.Extensions;
public static partial class LoggerExtensions
{
    [LoggerMessage(EventId = 0, Level = LogLevel.Information, Message = "{message}")]
    public static partial void LogMessage(this ILogger logger, string message);

    [LoggerMessage(EventId = 10, Message = "{message}")]
    public static partial void LogMessage(this ILogger logger, LogLevel logLevel, string message);

    [LoggerMessage(EventId = 110, Level = LogLevel.Error, Message = "{message}")]
    public static partial void LogException(this ILogger logger, string message, Exception exception);
}
