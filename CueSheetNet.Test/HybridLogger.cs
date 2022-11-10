using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Test;
internal class HybridLogger : ILogDevice
{
	public void WriteLog(LogEntry entry)
	{
        if (entry.Level < MinimumLogLevel) return;
		LogEntries.Add(entry);
		Console.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
	}

	public List<LogEntry> LogEntries = new();

	public LogLevel MinimumLogLevel => LogLevel.Debug;

	public HybridLogger()
	{
	}
}
