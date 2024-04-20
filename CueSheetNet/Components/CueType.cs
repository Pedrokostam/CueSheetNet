namespace CueSheetNet;

/// <summary>
/// Information about the CUE sheet.
/// </summary>
[Flags]
public enum CueType
{
    Unknown = 0,

    /// <summary>Single continuous file.</summary>
    SingleFile = 1 << 0,

    /// <summary>Multiple files.</summary>
    MultipleFiles = 1 << 1,

    /// <summary>Hidden Track One Audio.</summary>
    HTOA = 1 << 2,

    /// <summary>Uses Postgap or Pregap.</summary>
    SimulatedGaps = 1 << 3,

    /// <summary>Index 00 of track on previous file.</summary>
    InterfileGaps = 1 << 4,

    /// <summary>Contains audio tracks.</summary>
    Audio = 1 << 5,

    /// <summary>Contains data tracks.</summary>
    Data = 1 << 6,
}
