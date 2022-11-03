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
    public delegate void LogDelegate(LogLevel level, string msg);
    public delegate void LocateDelegate(string location);

    private static LogDelegate LogDelegateImpl = new (DummyWrite);
    private static LocateDelegate LocateDelegateImpl = new (DummySet);

    private static void DummyWrite(LogLevel lvl, string msg) { }
    private static void DummySet(string loc) { }

    public static LogDelegate Log => LogDelegateImpl;
    public static LocateDelegate Locate => LocateDelegateImpl;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="logBook"></param>
    public static void SetLogbook(Logbook logBook)
    {
        LogDelegateImpl = new(logBook.Log);
        LocateDelegateImpl = new(logBook.Locate);
    }
}
