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
		Console.WriteLine($"[{entry.Timestamp:HH:mm:ss:ffff}] {entry.Level} '{entry.Message}' in '{entry.Location}'");
		LogEntries.Add(entry);
	}
	public readonly Logbook DaBook = new ();
	public List<LogEntry> LogEntries = new();
	public HybridLogger()
	{
		Logger.SetLogbook(DaBook);
		DaBook.Register(this);
	}
}
