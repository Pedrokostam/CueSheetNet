using System.Runtime.CompilerServices;

namespace CueSheetNet;

///// <summary>
///// File types mentioned in the CUE sheet specification.
///// </summary>
//[Flags]
//public enum FileType
//{
//    /// <summary>
//    /// Not detected yet, or out-of-specification.
//    /// </summary>
//    Unknown = 0,

//    /// <summary>
//    /// Uncompressed audio data in a RIFF container (common on Windows). Also a catch-all for other audio formats not mentioned in specification, like FLAC.
//    /// </summary>
//    WAVE = 1 << 0,

//    /// <summary>
//    /// Uncompressed audio data in an IFF container (common on Apple).
//    /// </summary>
//    AIFF = 1 << 1,

//    /// <summary>
//    /// Compressed lossy audio data.
//    /// </summary>
//    MP3 = 1 << 2,

//    /// <summary>
//    /// Little-Endian binary.
//    /// </summary>
//    BINARY = 1 << 3,

//    /// <summary>
//    /// Big-Endian binary.
//    /// </summary>
//    MOTOROLA = 1 << 4,
//}

/// File types mentioned in the CUE sheet specification.
public readonly record struct FileType { 
    public string Name { get; }
    private FileType(string name)
    {
        Name = name;
    }
    public bool IsAudio => this == WAVE || this == AIFF || this == MP3;
    public bool IsData => this == BINARY || this == MOTOROLA;
    /// Uncompressed audio data in a RIFF container (common on Windows). Also a catch-all for other audio formats not mentioned in specification, like FLAC.
    public static FileType WAVE => new FileType("WAVE");
    /// Uncompressed audio data in an IFF container (common on Apple).
    public static FileType AIFF => new FileType("AIFF");
    /// Compressed lossy audio data.
    public static FileType MP3 => new FileType("MP3");
    /// Little-Endian binary.
    public static FileType BINARY => new FileType("BINARY");
    /// Big-Endian binary.
    public static FileType MOTOROLA => new FileType("MOTOROLA");
    /// Not detected yet, or out-of-specification.
    public static FileType Unknown(string value) => new FileType(value.ToUpperInvariant());
    public override string ToString() => Name;
}