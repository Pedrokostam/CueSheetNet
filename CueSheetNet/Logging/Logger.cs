﻿using System.Collections.ObjectModel;
using System.Diagnostics;

namespace CueSheetNet.Logging;
public class Logger
{
    private delegate void LogDelegateType(LogEntry entry);

    private readonly static List<ILogDevice> logDevices = new();
    public static ReadOnlyCollection<ILogDevice> LogDevices => logDevices.AsReadOnly();

    /// <summary>
    /// The minimum value of LogLevel that can be reported by at least one LogDevice
    /// </summary>
    public static LogLevel RequestedLogLevels { get; private set; } = LogLevel.None;

    /// <summary>
    /// Registers the specified <paramref name="device"/> as a log consumer. Updates <see cref="RequestedLogLevels"/> if necessary.
    /// </summary>
    /// <param name="device"><see cref="ILogDevice"/> to be registered</param>
    /// <returns>Number of registered log devices after the operation</returns>
    public static int Register(ILogDevice device)
    {
        logDevices.Add(device);
        RequestedLogLevels = logDevices.Min(x => x.RequestedLogLevels);
        Logger.LogDebug("Registered new logger with LogLevel: {Level}", device.RequestedLogLevels);
        return logDevices.Count;
    }
    /// <summary>
    /// Unregisters the specified <paramref name="device"/> removeing it from log consumers. Updates <see cref="RequestedLogLevels"/> if necessary.
    /// </summary>
    /// <param name="device"><see cref="ILogDevice"/> to be removed</param>
    /// <returns>Number of registered log devices after the operation</returns>
    public static int Unregister(ILogDevice device)
    {
        int index = LogDevices.IndexOf(device);
        if (index == -1)
            return logDevices.Count;
        LogDebug("Unregistering device {device} at index {index}", device, index);
        logDevices.RemoveAt(index);
        if (logDevices.Count == 0)
            RequestedLogLevels = LogLevel.None;
        else
            RequestedLogLevels = logDevices.Min(x => x.RequestedLogLevels);
        return logDevices.Count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns>True if level should be ignored, False if should be reported</returns>
    private static bool CheckLogLevelIgnored(LogLevel level)
    {
        bool logIgnored = !RequestedLogLevels.HasFlag(level);
#if DEBUG
        if (logIgnored)
            Debug.WriteLine("Skipped log due to level");
#endif
        return logIgnored;
    }
    private static void LogErrorBase(string message, Exception? errors, params object?[] args)
    {
        //if(errors is null)
        //{

        //    errors=Enumerable.Empty<Exception>();   
        //}
        //message += " Error: {Error}";
        //Log(LogLevel.Error, message, errors);

    }
    public static void LogWarning(string message) => Log(LogLevel.Warning, message);
    public static void LogWarning<T1>(string messageTemplate, T1 arg1) => Log(LogLevel.Warning, messageTemplate, arg1);
    public static void LogWarning<T1, T2>(string messageTemplate, T1 arg1, T2 arg2) => Log(LogLevel.Warning, messageTemplate, arg1, arg2);
    public static void LogWarning<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3) => Log(LogLevel.Warning, messageTemplate, arg1, arg2, arg3);
    public static void LogWarning<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Log(LogLevel.Warning, messageTemplate, arg1, arg2, arg3, arg4);
    public static void LogWarning<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => Log(LogLevel.Warning, messageTemplate, arg1, arg2, arg3, arg4, arg5);
    public static void LogWarning<T1, T2, T3, T4, T5, T6>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => Log(LogLevel.Warning, messageTemplate, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void LogWarning<T>(string messageTemplate, params T[] args) => Log(LogLevel.Warning, messageTemplate, args);
    public static void LogWarning(string messageTemplate, params object[] args) => Log(LogLevel.Warning, messageTemplate, args);
    public static void LogInformation(string message) => Log(LogLevel.Information, message);
    public static void LogInformation<T1>(string messageTemplate, T1 arg1) => Log(LogLevel.Information, messageTemplate, arg1);
    public static void LogInformation<T1, T2>(string messageTemplate, T1 arg1, T2 arg2) => Log(LogLevel.Information, messageTemplate, arg1, arg2);
    public static void LogInformation<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3) => Log(LogLevel.Information, messageTemplate, arg1, arg2, arg3);
    public static void LogInformation<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Log(LogLevel.Information, messageTemplate, arg1, arg2, arg3, arg4);
    public static void LogInformation<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => Log(LogLevel.Information, messageTemplate, arg1, arg2, arg3, arg4, arg5);
    public static void LogInformation<T1, T2, T3, T4, T5, T6>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => Log(LogLevel.Information, messageTemplate, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void LogInformation<T>(string messageTemplate, params T[] args) => Log(LogLevel.Information, messageTemplate, args);
    public static void LogInformation(string messageTemplate, params object[] args) => Log(LogLevel.Information, messageTemplate, args);
    public static void LogDebug(string message) => Log(LogLevel.Debug, message);
    public static void LogDebug<T1>(string messageTemplate, T1 arg1) => Log(LogLevel.Debug, messageTemplate, arg1);
    public static void LogDebug<T1, T2>(string messageTemplate, T1 arg1, T2 arg2) => Log(LogLevel.Debug, messageTemplate, arg1, arg2);
    public static void LogDebug<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3) => Log(LogLevel.Debug, messageTemplate, arg1, arg2, arg3);
    public static void LogDebug<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Log(LogLevel.Debug, messageTemplate, arg1, arg2, arg3, arg4);
    public static void LogDebug<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => Log(LogLevel.Debug, messageTemplate, arg1, arg2, arg3, arg4, arg5);
    public static void LogDebug<T1, T2, T3, T4, T5, T6>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => Log(LogLevel.Debug, messageTemplate, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void LogDebug<T>(string messageTemplate, params T[] args) => Log(LogLevel.Debug, messageTemplate, args);
    public static void LogDebug(string messageTemplate, params object[] args) => Log(LogLevel.Debug, messageTemplate, args);

    public static void Log(LogLevel level, string message)
    {
        if (CheckLogLevelIgnored(level))
            return;
        Log(level, message, Array.Empty<object?>());
    }
    public static void Log<T1>(LogLevel level, string messageTemplate, T1 arg1)
    {
        if (CheckLogLevelIgnored(level))
            return;
        object?[] objs = new object?[] { arg1 };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2)
    {
        if (CheckLogLevelIgnored(level))
            return;
        object?[] objs = new object?[] { arg1, arg2 };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
    {
        if (CheckLogLevelIgnored(level))
            return;
        object?[] objs = new object?[] { arg1, arg2, arg3 };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3, T4>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (CheckLogLevelIgnored(level))
            return;
        object?[] objs = new object?[] { arg1, arg2, arg3, arg4 };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3, T4, T5>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (CheckLogLevelIgnored(level))
            return;
        object?[] objs = new object?[] { arg1, arg2, arg3, arg4, arg5, };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3, T4, T5, T6>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (CheckLogLevelIgnored(level))
            return;
        object?[] objs = new object?[] { arg1, arg2, arg3, arg4, arg5, arg6 };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T>(LogLevel level, string messageTemplate, params T[] args)
    {
        if (CheckLogLevelIgnored(level))
            return;
        Log(level, messageTemplate, args.Cast<object?>().ToArray());
    }
    public static void Log(LogLevel level, string messageTemplate, params object?[] args)
    {
        if (CheckLogLevelIgnored(level))
            return;
        LogEntry meeting = new(level, messageTemplate, args);
        foreach (ILogDevice diwajs in logDevices)
        {
            if (diwajs.RequestedLogLevels.HasFlag(meeting.Level))
                diwajs.WriteLog(meeting);
        }
    }
}
