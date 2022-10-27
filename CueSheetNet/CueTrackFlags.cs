namespace CueSheetNet;

[Flags]
public enum CueTrackFlags
{
    None = 0b0,
    //DCP
    DigitalCopyPermitted = 0b001,
    //4CH
    FourChannel = 0b010,
    //PRE
    PreEmphasis = 0b100,
    //SCMS
    SerialCopyManagementSystem = 0b1000
}
