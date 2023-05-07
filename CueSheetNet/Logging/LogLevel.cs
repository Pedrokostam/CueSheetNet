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
    Debug = 1,
    /// <summary>
    /// Information about finishing sub-step
    /// </summary>
    Verbose = 2,
    /// <summary>
    /// Information about finishing of an important step
    /// </summary>
    Information = 4,
    /// <summary>
    /// Requested operation was performed, but with problems
    /// </summary>
    Warning = 8,
    /// <summary>
    /// Requested could not be performed
    /// </summary>
    Error = 16,
    All = Debug | Verbose | Information | Warning | Error,
}
