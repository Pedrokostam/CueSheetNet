using CueSheetNet.Reading;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Internal;

internal static partial class Log
{

    [LoggerMessage(EventId = 1, Message = "Specified file \"{file}\" does not exist")]
    public static partial void FileNotFound(ILogger logger, string file,LogLevel level=LogLevel.Error);

    [LoggerMessage(EventId = 1000, Message = "Parsing cue sheet from {source}")]
    public static partial void ParsingSheet(ILogger logger, CueSource source, LogLevel level = LogLevel.Information);
    [LoggerMessage(EventId = 1001, Message = "Finished parsing cue sheet in {elapsedMs:F2} ms")]
    public static partial void ParsedSheet(ILogger logger, double elapsedMs, LogLevel level = LogLevel.Information);
}
