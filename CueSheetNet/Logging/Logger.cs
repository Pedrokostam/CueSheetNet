using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;
/// <summary>
/// Static class which redirects log requests to the currently set <see cref="Logbook"/>
/// </summary>
public static class Logger
{
    private delegate void LogDelegateType(LogLevel level, string msg);
    private delegate void SetLocationDelegateType(LogLocation location);
    private delegate void SetContextDelegateType(string context);

    private static LogDelegateType LogDelegate = new(DummyWrite);
    private static SetContextDelegateType SetContextDelegate = new(DummySet);
    private static SetLocationDelegateType SetLocationDelegate = new(DummySet);

    private static void DummyWrite(LogLevel lvl, string msg) { }
    private static void DummySet(string loc) { }
    private static void DummySet(LogLocation loc) { }

    public static void Log(LogLevel level, string message) => LogDelegate(level, message);
    public static void SetLocation(LogLocation location) => SetLocationDelegate(location);
    public static void SetLocation(string obj, string context) => SetLocation(new LogLocation(obj, context));
    public static void SetObject(string obj) => SetLocation(obj,string.Empty);
    public static void SetContext(string context) => SetContextDelegate(context);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logBook"></param>
    public static void SetLogbook(Logbook logBook)
    {
        LogDelegate = new(logBook.Log);
        SetLocationDelegate = new(logBook.SetLocation);
        SetContextDelegate = new(logBook.SetContext);
    }
}
