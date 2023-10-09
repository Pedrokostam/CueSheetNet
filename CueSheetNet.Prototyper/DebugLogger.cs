using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
