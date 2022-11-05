namespace CueSheetNet.Logging;

public record struct LogLocation (string Object, string Context){
    public LogLocation():this(string.Empty,string.Empty)
    {
    }
    public static LogLocation NotSpecified => new("N/A", "N/A");
}
public record struct LogEntry(
                            DateTime Timestamp,
                            LogLevel Level,
                            string Message,
                            string Object,
                            string Context
                        )
{
    public LogEntry(LogLevel level, string message, LogLocation location):this(DateTime.Now,level,message,location.Object,location.Context)
    {

    }
}
