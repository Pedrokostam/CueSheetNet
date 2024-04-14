namespace CueSheetNet.Logging;

public class ConsoleLogger(LogLevel requested) : ArrayLogger(requested)
{
    public override void WriteLog(LogEntry entry)
    {
        base.WriteLog(entry);
        Console.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
    }
}
