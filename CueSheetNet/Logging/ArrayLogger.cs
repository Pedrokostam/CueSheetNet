using System.Collections.ObjectModel;

namespace CueSheetNet.Logging;

public class ArrayLogger : ILogDevice
{
    public virtual void WriteLog(LogEntry entry)
    {
        _LogEntries.Add(entry);
    }

    private List<LogEntry> _LogEntries = new();
    public ReadOnlyCollection<LogEntry> LogEntries => _LogEntries.AsReadOnly();

    private LogLevel _RequestedLogLevels;
    public LogLevel RequestedLogLevels => _RequestedLogLevels;

    public ArrayLogger(LogLevel requested)
    {
        _RequestedLogLevels = requested;

    }
}