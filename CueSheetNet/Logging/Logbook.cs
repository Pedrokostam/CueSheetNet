using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;


/// <summary>
///  Sinks for all log entries. After setting as the active sink (with <see cref="Logger.SetLogbook(CueSheetNet.Logging.Logbook)"/>, it will receive all logs made through <see cref="Logger"/>.
///  The logs will be then redirected to every registered <see cref="ILogDevice"/>, as well as to the inner master log, accesible through <see cref="GetAll(LogLevel)"/>.
/// </summary>
public class Logbook
{
    protected readonly List<LogEntry> masterLog = new();
    protected readonly List<ILogDevice> logDevices = new();
    public LogLevel MaxLogLevelEnabled { get; private set; }
    public Logbook()
    {
    }
    /// <summary>
    /// Create a log and adds it to the master log. Sends it to all registered <see cref="ILogDevice"/>s for processing.
    /// </summary>
    /// <param name="level"></param>
    /// <param name="msg"></param>
    public void Log(LogEntry logEntry)
    {
        lock (masterLog)
        {
            masterLog.Add(logEntry);
        }
        foreach (ILogDevice device in logDevices)
        {
            device.WriteEntry(logEntry);
        }
    }
    /// <summary>
    /// Clears all logs from master log
    /// </summary>
    public void Clear()
    {
        lock (masterLog)
            masterLog.Clear();
    }
    /// <summary>
    /// Gets all logs from the master log matching the specified mask.
    /// </summary>
    /// <param name="levelMask"></param>
    /// <returns>List containg every matching <see cref="LogEntry"/></returns>
    public List<LogEntry> GetAll(LogLevel levelMask = LogLevel.Standard)
    {
        List<LogEntry> result = new();
        lock (masterLog)
        {
            foreach (LogEntry entry in masterLog)
            {
                if (levelMask.HasFlag(entry.Level))
                    result.Add(entry);
            }
        }
        return result;
    }
    /// <summary>
    /// Registers the given <see cref="ILogDevice"/> making it process every incoming log.
    /// </summary>
    /// <param name="device"></param>
    public void Register(ILogDevice device)
    {
        if (device.MaxLogLevelEnabled > MaxLogLevelEnabled)
            MaxLogLevelEnabled = device.MaxLogLevelEnabled;
        logDevices.Add(device);
    }
    /// <summary>
    /// Deregisters the given <see cref="ILogDevice"/>, so that no logs are processed by it/
    /// </summary>
    /// <param name="device"></param>
    public void Unregister(ILogDevice device)
    {
        logDevices.Remove(device);
    }
}
