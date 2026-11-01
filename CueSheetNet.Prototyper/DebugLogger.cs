using CueSheetNet.Logging;
using CueSheetNet.Test;

namespace CueSheetNet.Test;
internal class DebugLogger : ArrayLogger
{
    override public void WriteLog(LogEntry entry)
    {
        base.WriteLog(entry);
        DebugHelper.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
    }

    public DebugLogger(LogLevel requsted) : base(requsted)
    {

    }
}
