namespace CueSheetNet;


[Flags]
public enum FileType
{
    Unknown = 0,
    WAVE = 1 << 0,
    AIFF = 1 << 1,
    MP3 = 1 << 2,
    /// <summary>Little-Endian binary</summary>
    BINARY = 1 << 3,
    /// <summary>Big-Endian binary</summary>
    MOTOROLA = 1 << 4
}

