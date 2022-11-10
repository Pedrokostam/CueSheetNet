namespace CueSheetNet.Logging;


/// <summary>
/// Represents an object that can process incoming log entries
/// </summary>
public interface ILogDevice
{
    /// <summary>
    /// Processes the given entry, for example by displaying it in the Console, or storing in a collection
    /// </summary>
    /// <param name="entry"></param>
    void WriteLog(LogEntry entry);
    /// <summary>
    /// The minimum value of LogLevel that can be reported by this ILogDevice
    /// </summary>
    LogLevel MinimumLogLevel { get; }
}