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
    #region Loggers
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
    [AllowNull]
    public static ILogger AudioReadingLogger { get; set => field = value ?? NullLogger.Instance; } = NullLogger.Instance;
    #endregion // Loggers
    public static void SetLogger(ILogger? logger)
    {
        ParsingLogger = logger;
        EditingLogger = logger;
        EncodingLogger = logger;
        WritingLogger = logger;
        AudioReadingLogger = logger;
        FileOpsLogger = logger;
    }
    #region Common 0-100
    [LoggerMessage(EventId = 0, Message = "Started {method}")]
    public static partial void StartedStep(this ILogger logger, string method, LogLevel level = LogLevel.Trace);

    [LoggerMessage(EventId = 1, Message = "Finished {method} in {milliseconds:f3} ms")]
    public static partial void FinishedStepMs(this ILogger logger, string method, double milliseconds, LogLevel level = LogLevel.Trace, Exception? exception = null);

    [LoggerMessage(EventId = 2, Message = "Finished {method} in {microseconds:f1} us")]
    public static partial void FinishedStepUs(this ILogger logger, string method, double microseconds, LogLevel level = LogLevel.Trace, Exception? exception = null);

    [LoggerMessage(EventId = 5, Level = LogLevel.Information, Message = "Started parsing cue sheet from {source}")]
    public static partial void StartedParsing(this ILogger logger, CueSource source);

    [LoggerMessage(EventId = 6, Level = LogLevel.Information, Message = "Ended parsing cue sheet from {source} in {milliseconds:F3} s")]
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
    #endregion // common 0-100

    #region Parsing 100-200
    [LoggerMessage(EventId = 100, Message = "Invalid {keyword} at line {number}: \"{line}\"")]
    public static partial void InvalidKeyword(this ILogger logger, string keyword, int number, string line, LogLevel level = LogLevel.Warning);
    [LoggerMessage(EventId = 101, Message = "INDEX line present before any track at line {number}: \"{line}\"")]
    public static partial void PrematureIndex(this ILogger logger, int number, string line, LogLevel level = LogLevel.Warning);
    [LoggerMessage(EventId = 102, Message = "GAP line present before any track at line {number}: \"{line}\"")]
    public static partial void PrematureGap(this ILogger logger, int number, string line, LogLevel level = LogLevel.Warning);
    #endregion // Parsing 100-200

    #region TextEncoding 300-400
    [LoggerMessage(EventId = 300, Level = LogLevel.Debug, Message = "Using non-standard encoding multi-byte encoding without byte order mark: {encodingName}")]
    public static partial void NonStandardEncoding(this ILogger logger, string encodingName);
    #endregion // TextEncoding 300-400

    #region NamePattern 400-500
    [LoggerMessage(EventId = 400, Level = LogLevel.Warning, Message = "Name parsing for pattern {pattern} resulted in empty string - replacing with old name {oldName}")]
    public static partial void EmptyParseForPattern(this ILogger logger, string pattern, string oldName);
    [LoggerMessage(EventId = 401, Level = LogLevel.Warning, Message = "No matching property found for {property} when parsing tree format {format}")]
    public static partial void NoMatchingParseProperty(this ILogger logger, string property, string format);
    #endregion // NamePattern

    #region Writing 500-600
    [LoggerMessage(EventId = 0,Level =LogLevel.Debug, Message = "Skipped writing empty field remark {field}")]
    public static partial void SkippedEmptyFieldRemark(this ILogger logger, string field);
    #endregion // Writing 500-600

    #region FileOps 600-700

    [LoggerMessage(EventId = 600, Level = LogLevel.Information, Message = "Copied file from {from} to {to}")]
    public static partial void CopiedFile(this ILogger logger, string from, string to);
    [LoggerMessage(EventId = 610, Level = LogLevel.Information, Message = "Removed file {file}")]
    public static partial void RemovedFile(this ILogger logger, string file);
    [LoggerMessage(EventId = 611, Level = LogLevel.Information, Message = "Removed file {file} (copy rollback)")]
    public static partial void RemovedFileRollback(this ILogger logger, string file);

    #endregion // FileOps 600-700

    #region AudioReading 10000-...
    [LoggerMessage(EventId = 10000, Level = LogLevel.Information, Message = "Added audio reader for format {format}")]
    public static partial void AudioReaderAdded(this ILogger logger, string format);

    [LoggerMessage(EventId = 10001, Level = LogLevel.Information, Message = "Reset audio readers to base state")]
    public static partial void AudioReadersReset(this ILogger logger);

    [LoggerMessage(EventId = 10002, Level = LogLevel.Debug, Message = "Successfully read metadata of \"{path}\" using {reader} reader")]
    public static partial void AudioReadingSuccess(this ILogger logger, string path, string reader);

    [LoggerMessage(EventId = 10002, Level = LogLevel.Warning, Message = "Could not read audio file metadata of \"{path}\" using {reader} reader")]
    public static partial void AudioReadingFail(this ILogger logger, string path, string reader, Exception? exception);

    [LoggerMessage(EventId = 10010, Level = LogLevel.Debug, Message = "CD Data stream has mismatched header ({trackType})")]
    public static partial void CdFormatMismatchedHeader(this ILogger logger, TrackType trackType);

    [LoggerMessage(EventId = 10020, Level = LogLevel.Debug, Message = "Mismatched declared file length vs actual")]
    public static partial void AiffLengthMismatch(this ILogger logger);

    [LoggerMessage(EventId = 10030, Level = LogLevel.Warning, Message = "Could not find ffprobe using path: '{expectedPath}' - file metadata not read. Specify path to ffprobe using {className}.{propName}")]
    public static partial void FfprobeMissingExecutable(this ILogger logger, string expectedPath, string className, string propName);
    [LoggerMessage(EventId = 10031, Level = LogLevel.Error, Message = "Could not read metadata")]
    public static partial void FfprobeError(this ILogger logger, Exception? exception);

    
    #endregion // AudioReading 10000-...
}
