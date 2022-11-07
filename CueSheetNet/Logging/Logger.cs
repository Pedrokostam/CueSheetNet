using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;


[InterpolatedStringHandler]
public ref struct LogInterpolatedStringHandler
{
    // Storage for the built-up string
    StringBuilder builder;
    private readonly bool enabled;
    public int LiteralLength =>builder.Length;
    public LogInterpolatedStringHandler(int literalLength, int formattedCount,LogLevel level)
    {
        enabled = !(Logger.Attached is null || Logger.Attached.MaxLogLevelEnabled < level);
        builder = new StringBuilder(literalLength);
    }

    public void AppendLiteral(string s)
    {
        if (!enabled) return;
        //Console.WriteLine($"\tAppendLiteral called: {{{s}}}");

        builder.Append(s);
        //Console.WriteLine($"\tAppended the literal string");
    }

    public void AppendFormatted<T>(T t)
    {
        if (!enabled) return;
        //Console.WriteLine($"\tAppendFormatted called: {{{t}}} is of type {typeof(T)}");

        builder.Append(t?.ToString());
        //Console.WriteLine($"\tAppended the formatted object");
    }

    internal string GetFormattedText()
    {
        return builder.ToString();
    }
}
/// <summary>
/// Static class which redirects log requests to the currently set <see cref="Logbook"/>
/// </summary>
public static class Logger
{
    private delegate void LogDelegateType(LogEntry entry);

    private static LogDelegateType LogDelegate = new(DummyWrite);
    internal static Logbook? Attached { get; set; }
    private static void DummyWrite(LogEntry e) { }
    public static void Log(LogEntry entry) => LogDelegate(entry);
    public static void Log(LogLevel level, string message, LogLocation location) => LogDelegate(new(level, message, location));
    public static void Log(LogLevel level, string message, string source, string context) => LogDelegate(new(level, message, new(source, context)));
    public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler message, string source, string context)
    {
        if (level < Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message.GetFormattedText(), new(source, context)));
    }
    public static void Log(LogLevel level, string message, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler source, string context)
    {
        if (level < Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message, new( source.GetFormattedText(),context)));
    }
    public static void Log(LogLevel level, string message, string source, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler context)
    {
        if (level <= Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message, new(source, context.GetFormattedText())));
    }
    public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler message, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler source, string context)
    {
        if (level < Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message.GetFormattedText(), new(source.GetFormattedText(), context)));
    }
    public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler message, string source, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler context)
    {
        if (level < Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message.GetFormattedText(), new(source, context.GetFormattedText())));
    }
    public static void Log(LogLevel level, string message, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler source, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler context)
    {
        if (level < Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message, new(source.GetFormattedText(), context.GetFormattedText())));
    }
    public static void Log(LogLevel level, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler message, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler source, [InterpolatedStringHandlerArgument("level")] LogInterpolatedStringHandler context)
    {
        if (level < Attached?.MaxLogLevelEnabled)
            LogDelegate(new(level, message.GetFormattedText(), new(source.GetFormattedText(), context.GetFormattedText())));
    }


    public static bool IsLogbookAttached => Attached != null;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logBook"></param>
    public static void SetLogbook(Logbook logBook)
    {
        Attached = logBook;
        LogDelegate = new(logBook.Log);
    }
    public static void ResetLogbook()
    {
        Attached = null;
        LogDelegate = new LogDelegateType(DummyWrite);
    }
}
