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
    void WriteEntry(LogEntry entry);
    LogLevel MaxLogLevelEnabled { get; }
}