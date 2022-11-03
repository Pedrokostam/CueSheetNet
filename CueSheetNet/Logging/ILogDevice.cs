namespace CueSheetNet.Logging;

public interface ILogDevice
{
    void WriteEntry(LogEntry entry);
}