using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;
/// <summary>
/// Enumeration with possible log levels. The more important/severe the level is, the higher value it has.
/// </summary>
[Flags]
public enum LogLevel
{
    None = 0,
    /// <summary>
    /// Information about current state of step/sub-step
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Information about finishing of an important step
    /// </summary>
    Information = 2,
    /// <summary>
    /// Requested operation was performed, but with problems
    /// </summary>
    Debug = 4,



    All = Debug | Information | Warning,
}
