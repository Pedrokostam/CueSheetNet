using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;
public static class Logger
{
    private delegate void LogDelegateType(LogEntry entry);

    private readonly static List<ILogDevice> logDevices = new();
    public static ReadOnlyCollection<ILogDevice> LogDevices => logDevices.AsReadOnly();

    /// <summary>
    /// The minimum value of LogLevel that can be reported by at least one LogDevice
    /// </summary>
    public static LogLevel MinimumLogLevel { get; private set; } = LogLevel.Off;

    /// <summary>
    /// Registers the specified <paramref name="device"/> as a log consumer. Updates <see cref="MinimumLogLevel"/> if necessary.
    /// </summary>
    /// <param name="device"><see cref="ILogDevice"/> to be registered</param>
    /// <returns>Number of registered log devices after the operation</returns>
    public static int Register(ILogDevice device)
    {
        logDevices.Add(device);
        MinimumLogLevel = logDevices.Min(x => x.MinimumLogLevel);
        return logDevices.Count;
    }
    /// <summary>
    /// Unregisters the specified <paramref name="device"/> removeing it from log consumers. Updates <see cref="MinimumLogLevel"/> if necessary.
    /// </summary>
    /// <param name="device"><see cref="ILogDevice"/> to be removed</param>
    /// <returns>Number of registered log devices after the operation</returns>
    public static int Unregister(ILogDevice device)
    {
        if (logDevices.Remove(device))
        {
            if (logDevices.Count == 0)
                MinimumLogLevel = LogLevel.Off;
            else
                MinimumLogLevel = logDevices.Min(x => x.MinimumLogLevel);
        }
        return logDevices.Count;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="level"></param>
    /// <returns>True if level should be ignored, False if should be reported</returns>
    private static bool CheckLevelIgnored(LogLevel level)
    {
#if DEBUG
        bool git = MinimumLogLevel > level;
        if (git)
            Debug.WriteLine("Skipped log due to level");
        return git;
#else
        return MinimumLogLevel > level;
#endif
    }
    public static void LogError(string message) => Log(LogLevel.Error, message);
    public static void LogError<T1>(string messageTemplate, T1 arg1) => Log(LogLevel.Error, messageTemplate, arg1);
    public static void LogError<T1, T2>(string messageTemplate, T1 arg1, T2 arg2) => Log(LogLevel.Error, messageTemplate, arg1, arg2);
    public static void LogError<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3) => Log(LogLevel.Error, messageTemplate, arg1, arg2, arg3);
    public static void LogError<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Log(LogLevel.Error, messageTemplate, arg1, arg2, arg3, arg4);
    public static void LogError<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => Log(LogLevel.Error, messageTemplate, arg1, arg2, arg3, arg4, arg5);
    public static void LogError<T1, T2, T3, T4, T5, T6>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => Log(LogLevel.Error, messageTemplate, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void LogError<T>(string messageTemplate, params T[] args) => Log(LogLevel.Error, messageTemplate, args);
    public static void LogError(string messageTemplate, params object[] args) => Log(LogLevel.Error, messageTemplate, args);
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
    public static void LogVerbose(string message) => Log(LogLevel.Verbose, message);
    public static void LogVerbose<T1>(string messageTemplate, T1 arg1) => Log(LogLevel.Verbose, messageTemplate, arg1);
    public static void LogVerbose<T1, T2>(string messageTemplate, T1 arg1, T2 arg2) => Log(LogLevel.Verbose, messageTemplate, arg1, arg2);
    public static void LogVerbose<T1, T2, T3>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3) => Log(LogLevel.Verbose, messageTemplate, arg1, arg2, arg3);
    public static void LogVerbose<T1, T2, T3, T4>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4) => Log(LogLevel.Verbose, messageTemplate, arg1, arg2, arg3, arg4);
    public static void LogVerbose<T1, T2, T3, T4, T5>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) => Log(LogLevel.Verbose, messageTemplate, arg1, arg2, arg3, arg4, arg5);
    public static void LogVerbose<T1, T2, T3, T4, T5, T6>(string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) => Log(LogLevel.Verbose, messageTemplate, arg1, arg2, arg3, arg4, arg5, arg6);
    public static void LogVerbose<T>(string messageTemplate, params T[] args) => Log(LogLevel.Verbose, messageTemplate, args);
    public static void LogVerbose(string messageTemplate, params object[] args) => Log(LogLevel.Verbose, messageTemplate, args);
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
        if (CheckLevelIgnored(level))
            return;
        Log(level, message, Array.Empty<object>());
    }
    public static void Log<T1>(LogLevel level, string messageTemplate, T1 arg1)
    {
        if (CheckLevelIgnored(level))
            return;
        object[] objs = new object[] { arg1! };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2)
    {
        if (CheckLevelIgnored(level))
            return;
        object[] objs = new object[] { arg1!, arg2! };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3)
    {
        if (CheckLevelIgnored(level))
            return;
        object[] objs = new object[] { arg1!, arg2!, arg3! };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3, T4>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
        if (CheckLevelIgnored(level))
            return;
        object[] objs = new object[] { arg1!, arg2!, arg3!, arg4! };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3, T4, T5>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
        if (CheckLevelIgnored(level))
            return;
        object[] objs = new object[] { arg1!, arg2!, arg3!, arg4!, arg5!, };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T1, T2, T3, T4, T5, T6>(LogLevel level, string messageTemplate, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
        if (CheckLevelIgnored(level))
            return;
        object[] objs = new object[] { arg1!, arg2!, arg3!, arg4!, arg5!, arg6! };
        Log(level, messageTemplate, objs);
    }
    public static void Log<T>(LogLevel level, string messageTemplate, params T[] args)
    {
        if (CheckLevelIgnored(level))
            return;
        Log(level, messageTemplate, args.Cast<object>().ToArray());
    }
    public static void Log(LogLevel level, string messageTemplate, params object[] args)
    {
        if (CheckLevelIgnored(level))
            return;
        LogEntry meeting = new(level, messageTemplate, args);
        foreach (ILogDevice diwajs in logDevices)
        {
            if (diwajs.MinimumLogLevel <= meeting.Level)
                diwajs.WriteLog(meeting);
        }
    }
}
