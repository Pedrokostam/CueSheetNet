namespace CueSheetNet.Logging;
public class ConsoleLogger : ArrayLogger
{
    override public void WriteLog(LogEntry entry)
    {
        base.WriteLog(entry);
        Console.WriteLine($"{entry.Timestamp:HH:mm:ss.fff} [{entry.Level}] {entry.Message}");
    }

    public ConsoleLogger(LogLevel requested) : base(requested)
    {
    }
}
