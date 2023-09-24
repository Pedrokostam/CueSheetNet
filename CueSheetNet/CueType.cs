namespace CueSheetNet;

[Flags]
public enum CueType
{
    Unknown = 0,

    /// <summary>Single continuous file</summary>
    SingleFile = 0b1,

    /// <summary>Multiple files</summary>
    MultipleFiles = 0b10,

    /// <summary>Gaps trimmed from files and simulated</summary>
    SimulatedGaps = 0b1000,

    /// <summary>Gaps appended to previous tracks</summary>
    GapsAppended = 0b10000,

    /// <summary>Gaps prepended to its tracks</summary>
    GapsPrepended = 0b100000,

    /// <summary>Hidden Track One Audio</summary>
    HTOA = 0b1000000,

    /// <summary>Gaps appended to next tracks</summary>
    EacStyle = MultipleFiles | GapsAppended,

    //MultipleFilesWithAppendedGaps = EacStyle,
    //SingleFileWithHiddenTrackOneAudio = SingleFile | HTOA,
    //MultipleFilesWithPrependedGaps = MultipleFiles | GapsPrepended,
    //MultipleFileWithSimulatedGaps = MultipleFiles | SimulatedGaps,
}
