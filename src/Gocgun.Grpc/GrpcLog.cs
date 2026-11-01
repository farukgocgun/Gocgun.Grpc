using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Gocgun.Grpc;

internal static partial class GrpcLog
{
    public static void Completed(ILogger logger, string method, string client, string status, long startedTimestamp, string messageId)
    {
        if (!logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        var durationMs = (long)Stopwatch.GetElapsedTime(startedTimestamp).TotalMilliseconds;
        CompletedCore(logger, method, client, status, durationMs, messageId);
    }

    [LoggerMessage(EventId = 2, Level = LogLevel.Error, Message = "gRPC method {Method} status {Status} exception_type {ExceptionType}")]
    public static partial void Failed(ILogger logger, string method, string status, string exceptionType);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "gRPC method {Method} ended early with status {Status}")]
    public static partial void Aborted(ILogger logger, string method, string status);

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "gRPC method {Method} client {Client} status {Status} duration_ms {DurationMs} message_id {MessageId}")]
    private static partial void CompletedCore(ILogger logger, string method, string client, string status, long durationMs, string messageId);
}
