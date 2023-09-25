namespace CueSheetNet;

[Flags]
public enum CueType
{
    Unknown = 0,

    /// <summary>Single continuous file</summary>
    SingleFile = 0b00000001,

    /// <summary>Multiple files</summary>
    MultipleFiles = 0b00000010,

    /// <summary>Hidden Track One Audio</summary>
    HTOA = 0b00000100,

    /// <summary>Uses Postgap or Pregap</summary>
    SimulatedGaps = 0b00001000,

    /// <summary>Index 00 of track on previous file</summary>
    InterfileGaps = 0b00010000,
}
