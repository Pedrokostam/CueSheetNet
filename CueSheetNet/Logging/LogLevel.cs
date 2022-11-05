using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueSheetNet.Logging;
[Flags]
public enum LogLevel
{
    None = 0,
    /// <summary>
    /// Requested could not be performed
    /// </summary>
    Error = 1,
    /// <summary>
    /// Requested operation was performed, but with problems
    /// </summary>
    Warning = 2,
    /// <summary>
    /// Information about finishing of an important step
    /// </summary>
    Information = 4,
    /// <summary>
    /// Information about finishing sub-step
    /// </summary>
    Verbose = 8,
    /// <summary>
    /// Information about current state of step/sub-step
    /// </summary>
    Debug = 16,
    All = Error | Warning | Information | Debug,
    Standard =  Error | Warning | Information
}
