namespace CueSheetNet.Logging;

public record struct LogLocation (string Source, string Context){
    public LogLocation():this(string.Empty,string.Empty)
    {
    }
}
public record struct LogEntry(
                            DateTime Timestamp,
                            LogLevel Level,
                            string Message,
                            LogLocation Location
                        )
{
    public LogEntry(LogLevel level, string message, string source, string context):this(DateTime.Now,level,message,new(source,context))
    {

    }
    public LogEntry(LogLevel level, string message, LogLocation location) : this(DateTime.Now, level, message, location)
    {

    }
}
