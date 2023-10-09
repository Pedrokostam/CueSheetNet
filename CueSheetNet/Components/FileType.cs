namespace CueSheetNet;


[Flags]
public enum FileType
{
    Unknown = 0,
    WAVE = 0b1,
    AIFF = 0b10,
    MP3 = 0b100,
    /// <summary>Little-Endian binary</summary>
    BINARY = 0b1000,
    /// <summary>Big-Endian binary</summary>
    MOTOROLA = 0b10000
}

