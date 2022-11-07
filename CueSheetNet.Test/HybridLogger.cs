using CueSheetNet.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Test;
internal class HybridLogger : ILogDevice
{
    public void WriteEntry(LogEntry entry)
	{
		if (entry.Level > MaxLogLevelEnabled) return;
		Console.WriteLine($"[{entry.Timestamp:HH:mm:ss:ffff}] {entry.Level} - {entry.Message}");
		Console.WriteLine($"                {entry.Location.Source} / {entry.Location.Context}");
		LogEntries.Add(entry);
	}
	public readonly Logbook DaBook = new ();
	public List<LogEntry> LogEntries = new();

	public LogLevel MaxLogLevelEnabled => LogLevel.Debug;

	public HybridLogger()
	{
		Logger.SetLogbook(DaBook);
		DaBook.Register(this);
	}
}
