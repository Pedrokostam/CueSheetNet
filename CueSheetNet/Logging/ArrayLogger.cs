using System.Collections.ObjectModel;

namespace CueSheetNet.Logging;

public class ArrayLogger(LogLevel requested) : ILogDevice
{
    public virtual void WriteLog(LogEntry entry)
    {
        _LogEntries.Add(entry);
    }

    private readonly List<LogEntry> _LogEntries = [];
    public ReadOnlyCollection<LogEntry> LogEntries => _LogEntries.AsReadOnly();

    protected LogLevel _RequestedLogLevels = requested;
    public LogLevel RequestedLogLevels => _RequestedLogLevels;

    public Guid InstanceId { get; } = Guid.NewGuid();
}