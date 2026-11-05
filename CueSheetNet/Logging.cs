using CueSheetNet.Reading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet;

public static partial class Loggingq
{
    [AllowNull]
    public static ILogger ParsingLogger { get; set => field = value ?? NullLogger.Instance; } = NullLogger.Instance;
    [AllowNull]
    public static ILogger EncodingLogger { get; set => field = value ?? NullLogger.Instance; } = NullLogger.Instance;
    [AllowNull]
    public static ILogger EditingLogger { get; set => field = value ?? NullLogger.Instance; } = NullLogger.Instance;
    [AllowNull]
    public static ILogger WritingLogger { get; set => field = value ?? NullLogger.Instance; } = NullLogger.Instance;
    [AllowNull]
    public static ILogger FileOpsLogger { get; set => field = value ?? NullLogger.Instance; } = NullLogger.Instance;
    public static void SetLogger(ILogger? logger)
    {
        ParsingLogger = logger;
        EditingLogger = logger;
        EncodingLogger = logger;
        WritingLogger = logger;
        FileOpsLogger = logger;
    }
    [LoggerMessage(EventId = 10, Message = "Started {method}")]
    public static partial void StartedStep(this ILogger logger, string method, LogLevel level = LogLevel.Trace);

    [LoggerMessage(EventId = 11, Message = "Finished {method} in {milliseconds:f3} ms")]
    public static partial void FinishedStepMs(this ILogger logger, string method, double milliseconds, LogLevel level = LogLevel.Trace, Exception? exception=null);

    [LoggerMessage(EventId = 12, Message = "Finished {method} in {microseconds:f1} us")]
    public static partial void FinishedStepUs(this ILogger logger, string method, double microseconds, LogLevel level = LogLevel.Trace, Exception? exception = null);

    [LoggerMessage(EventId = 15, Level = LogLevel.Information, Message = "Started parsing cue sheet from {source}")]
    public static partial void StartedParsing(this ILogger logger, CueSource source);

    [LoggerMessage(EventId = 16, Level = LogLevel.Information, Message = "Ended parsing cue sheet from {source} in {milliseconds:F3} ms")]
    public static partial void EndedParsing(this ILogger logger, CueSource source, double milliseconds, Exception? exception = null);

    public static void FinishedStep(this ILogger logger, string method, TimeSpan timespan, LogLevel level = LogLevel.Trace, Exception? exception = null)
    {
        if ((int)timespan.TotalMilliseconds > 0)
        {
            logger.FinishedStepMs(method, timespan.TotalMilliseconds, level, exception);
        }
        else
        {
            logger.FinishedStepUs(method, timespan.TotalMicroseconds, level, exception);
        }
    }
    /**/

    [LoggerMessage(EventId = 100, Message = "Invalid {keyword} at line {number}: \"{line}\"")]
    public static partial void InvalidKeyword(this ILogger logger, string keyword, int number, string line, LogLevel level = LogLevel.Warning);
    [LoggerMessage(EventId = 101, Message = "INDEX line present before any track at line {number}: \"{line}\"")]
    public static partial void PrematureIndex(this ILogger logger, int number, string line, LogLevel level = LogLevel.Warning);
    [LoggerMessage(EventId = 102, Message = "GAP line present before any track at line {number}: \"{line}\"")]
    public static partial void PrematureGap(this ILogger logger, int number, string line, LogLevel level = LogLevel.Warning);
}
