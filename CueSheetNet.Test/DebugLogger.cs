using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Test;
internal class DebugLogger : ILogDevice
{
    public void WriteLog(LogEntry entry)
    {
        LogEntries.Add(entry);
        Console.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
    }

    public List<LogEntry> LogEntries = new();


    private LogLevel _RequestedLogLevels;
    public LogLevel RequestedLogLevels => _RequestedLogLevels;

    public DebugLogger(LogLevel requsted)
    {
        _RequestedLogLevels = requsted;

    }
}
