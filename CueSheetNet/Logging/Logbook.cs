using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;

public class Logbook
{
    protected readonly List<LogEntry> logs = new();
    protected readonly List<ILogDevice> logDevices = new();
    protected readonly Dictionary<int, string> locations = new();

    

    public Logbook()
    {
        locations[Environment.CurrentManagedThreadId] = string.Empty;
    }
    public void Locate(string location)
    {
        lock(locations)
            locations[Environment.CurrentManagedThreadId] = location;
    }
    public void Log(LogLevel level, string msg)
    {
        LogEntry entry = new()
        {
            Timestamp = DateTime.Now,
            Level = level,
            Message = msg,
            Location = locations.GetValueOrDefault(Environment.CurrentManagedThreadId) ?? "N/A"
        };
        lock (logs)
        {
            logs.Add(entry);
        }

        foreach (ILogDevice device in logDevices)
        {
            device.WriteEntry(entry);
        }
    }

    public void Clear()
    {
        lock (logs)
            logs.Clear();
    }
    public List<LogEntry> GetAll(LogLevel levelMask = LogLevel.Standard)
    {
        List<LogEntry> result = new();
        lock (logs)
        {
            foreach (LogEntry entry in logs)
            {
                if(levelMask.HasFlag(entry.Level))
                    result.Add(entry);
            }
        }
        return result;
    }

    public void Register(ILogDevice device)
    {
        logDevices.Add(device);
    }
    public void Unregister(ILogDevice device)
    {
        logDevices.Remove(device);
    }
}
