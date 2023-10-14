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
    Warning = 1 << 0,

    /// <summary>
    /// Information about finishing of an important step
    /// </summary>
    Information = 1 << 1,
    /// <summary>
    /// Requested operation was performed, but with problems
    /// </summary>
    Debug = 1 << 2,



    All = Debug | Information | Warning,
}
