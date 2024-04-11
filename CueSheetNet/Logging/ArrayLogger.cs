using System.Collections.ObjectModel;

namespace CueSheetNet.Logging;

public class ArrayLogger : ILogDevice
{
    public virtual void WriteLog(LogEntry entry)
    {
        _LogEntries.Add(entry);
    }

    private readonly List<LogEntry> _LogEntries = [];
    public ReadOnlyCollection<LogEntry> LogEntries => _LogEntries.AsReadOnly();

    protected LogLevel _RequestedLogLevels;
    public LogLevel RequestedLogLevels => _RequestedLogLevels;

    public Guid InstanceId { get; }

    public ArrayLogger(LogLevel requested)
    {
        InstanceId = Guid.NewGuid();
        _RequestedLogLevels = requested;
    }
}