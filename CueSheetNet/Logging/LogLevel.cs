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
    Fatal = 0b01,
    Error = 0b10,
    Warning = 0b100,
    Information = 0b1000,
    Debug = 0b10000,
    All = Fatal | Error | Warning | Information | Debug,
    Urgent = Fatal | Error,
    Standard = Fatal | Error | Warning | Information
}
public enum LogMask
{
    None = 0,
    Fatal = 0b01,
    Error = 0b10,
    Warning = 0b100,
    Information = 0b1000,
    Debug = 0b10000,
    All = Fatal | Error | Warning | Information | Debug,
    Urgent = Fatal | Error,
    Standard = Fatal | Error | Warning | Information
}
