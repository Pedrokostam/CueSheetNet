namespace CueSheetNet.Syntax;

[Flags]
public enum TrackFlags
{
    None = 0,
    //DCP
    DigitalCopyPermitted = 1 << 0,
    //4CH
    FourChannel = 1 << 1,
    //PRE
    PreEmphasis = 1 << 2,
    //SCMS
    SerialCopyManagementSystem = 1 << 3,
}
