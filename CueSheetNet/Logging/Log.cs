namespace CueSheetNet.Logging;

public record struct LogEntry(
                            DateTime Timestamp,
                            LogLevel Level,
                            string Message,
                            string Location
                        )
{
}
