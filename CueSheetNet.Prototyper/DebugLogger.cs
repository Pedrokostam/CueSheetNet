using CueSheetNet.Logging;
using System.Diagnostics;

namespace CueSheetNet.Test;
internal class DebugLogger : ArrayLogger
{
    override public void WriteLog(LogEntry entry)
    {
        base.WriteLog(entry);
        Debug.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
    }

    public DebugLogger(LogLevel requsted) : base(requsted)
    {

    }
}
